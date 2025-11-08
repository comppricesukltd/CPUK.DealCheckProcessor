using Amazon.Runtime.Internal.Transform;
using CPUK.Authorization.DataAccess.Context;
using CPUK.Authorization.Service;
using CPUK.BusinessLogic.Services;
using CPUK.Domain.Entities.DealCheck;
using CPUK.Domain.Entities.Hotel;
using CPUK.Domain.Entities.Util;
using CPUK.ExternalCommunication.Twilio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static CPUK.ConfigurationSettings.Settings;

namespace CPUK.DealCheckProcessor.App.Service
{
    public class UserMessagingService
    {
        private static class Emoji
        {
            public static readonly string WavingHandSign = "\U0001F44B";
            public static readonly string RoundPushpin = "\U0001F4CD";
            public static readonly string Airplane = "\u2708\uFE0F";
            public static readonly string Bed = "\U0001F6CF";
            public static readonly string ForkAndKnife = "\U0001F374";
            public static readonly string HourglassWithFlowingSand = "\u23F3\uFE0F";

            public static readonly string RightPointingMagnifyingGlass = "\U0001F50E";
            public static readonly string LeftPointingMagnifyingGlass = "\U0001F50D";
            public static readonly string SpeechBalloon = "\U0001F4AC";
            public static readonly string GlowingStar = "\U0001F31F";
            public static readonly string Star = "\u2B50\uFE0F";
            public static readonly string Sparklestar = "\u2728\uFE0F";
            public static readonly string WhiteHeavyCheckMark = "\u2705\uFE0F";
            public static readonly string LowerRightPencil = "✏️";
            public static readonly string WhiteRightPointingBackhandIndex = "\U0001F449";



        }

        private static readonly MagicLinkService magicLinkService = new MagicLinkService();
        private static readonly ShortLinkService shortLinkService = new ShortLinkService();
        private readonly Lazy<AuthService> authService = new Lazy<AuthService>(() => new AuthService(new SigninDbContext()));
        private readonly TTIHotelService TTIHotelService = new TTIHotelService();

        private readonly string PhoneNumber;
        private readonly TTIHotel HotelData;
        private readonly DealCheckRequest DealCheckRequest;
        private readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        private Task messagingTask;
        private CancellationToken CancellationToken => CancellationTokenSource.Token;
        public Lazy<BoundsRange<DateTime>> DateRange => new Lazy<BoundsRange<DateTime>>(() => new BoundsRange<DateTime>(DealCheckRequest.Criteria.DepartureDate.Value, DealCheckRequest.Criteria.DepartureDate.Value.AddDays(DealCheckRequest.Criteria.Duration.Value)));
        public UserMessagingService(DealCheckRequest dealCheckRequest)
        {

            PhoneNumber = authService.Value.GetUserPhoneNumber(dealCheckRequest.UserId);
            HotelData = TTIHotelService.GetHotels(dealCheckRequest.Criteria.TtiCode).FirstOrDefault();
            DealCheckRequest = dealCheckRequest;

        }

        private static string GetLinkForDealCheckRequest(int requestId, int userId, string phoneNumber, bool drillPage = true)
                //=> shortLinkService.CreatShortLink(new ShortLinkBuilder($"http://localhost:4200/results/{requestId}",
                => shortLinkService.CreatShortLink(new ShortLinkBuilder($"https://anybetter.com/results{(drillPage ? $"/{requestId}" : string.Empty)}",
                new Dictionary<string, string> {
                    { MagicLinkService.QP_KEY, magicLinkService.CreateMagicLinkToken(userId).ToString().ToLower() },
                    { "phoneNumber", phoneNumber },
                    { "code", "hesoyam" }
            }));
        private string GetLinkForDealCheckRequest(bool drillPage = true)
            => GetLinkForDealCheckRequest(DealCheckRequest.Id, DealCheckRequest.UserId, PhoneNumber, drillPage);

        public static void TestMLMessage(int userId, int requestId, string phoneNumber)
        {
            var magicLink = GetLinkForDealCheckRequest(requestId, userId, phoneNumber);

            TwilioService.SendWhatsappMessage(phoneNumber, $"Hey! Test link is there:\n{magicLink}", out _);
        }
        public void SendCompletedNotification()
            => TwilioService.SendWhatsappMessage(PhoneNumber,
                $"{Emoji.Sparklestar} All done!\n" +
                $"We’ve checked across all the top operators so you don’t have to.\n" +
                $"\n" +
                $"Your *{HotelData.Name}* results are ready - with every available {Emoji.Airplane}{Emoji.Bed} flight, room, and meal plan in one place.\n" +
                $"\n" +
                $"See your full set of live deals here:\n" +
                GetLinkForDealCheckRequest(), out _);


        public void StopUserMessaging()
        {
            if (messagingTask != null && !messagingTask.IsCompleted)
            {
                CancellationTokenSource.Cancel();
                try { messagingTask?.Wait(); } catch { }
            }
        }
        public void StartUserMessaging(Task<bool> HasDeals)
            => messagingTask = Task.Run(async () =>
            {
                if (!TwilioService.IsWhatsAppCSWindowOpen(PhoneNumber))
                {
                    //step 0: Aknowledge. Ask permission for sending messages
                    TwilioService.SendWhatsappMessage(PhoneNumber, string.Empty, out _, WhatsApp.AnyBetter.AknowledgeTemplateSid, new Dictionary<string, string>
                    {
                        { "hotel_name",HotelData.Name },
                        { "destination",$"{HotelData.Country}, {HotelData.Locale}, {HotelData.City}" }
                    });
                }

                await CheckCSWindowOpenOrWait();

                //step 1: Send initial message
                TwilioService.SendWhatsappMessage(PhoneNumber,
                    $"Hi {Emoji.WavingHandSign} we’ve got your request for *{HotelData.Name}* in {Emoji.RoundPushpin}*{HotelData.Country}, {HotelData.Locale}, {HotelData.City}* ({DateRange.Value.LeftBound:d-MMM-yy} → {DateRange.Value.RightBound:d-MMM-yy}).\n" +
                    "\n" +
                    $"We’re now checking {Emoji.Airplane} flights, {Emoji.Bed} rooms, and {Emoji.ForkAndKnife} meal plans across trusted operators.\n" +
                    $"Hang tight {Emoji.HourglassWithFlowingSand} - I’ll keep you posted and send your full results link here", out _);

                //step 2: Wait 30s
                await Task.Delay(TimeSpan.FromSeconds(30), CancellationToken);
                var validReviews = HotelData.ReviewList.Where(x => x.Rating >= 4).ToList();


                //If has reviews - lets send them
                if (validReviews.Any())
                {
                    var review1 = validReviews.ElementAtOrDefault(0);
                    var review2 = validReviews.ElementAtOrDefault(1);

                    await CheckCSWindowOpenOrWait();

                    //step 3: Send first review
                    TwilioService.SendWhatsappMessage(PhoneNumber,
                        $"{Emoji.RightPointingMagnifyingGlass} Still crunching the details *{HotelData.Name}*…\n" +
                        "\n" +
                        $"{Emoji.SpeechBalloon}Here’s what a recent guest had to say:\n" +
                        GetReviewSnipper(review1), out _);


                    var hasDeals = await HasDeals;
                    //Checking if there is second review
                    if (hasDeals && review2 != null)
                    {
                        //step 4: Wait 90s
                        //If competitors processing is done => cancel messaging 
                        await CheckCSWindowOpenOrWait();

                        //step 5: Send second review
                        TwilioService.SendWhatsappMessage(PhoneNumber,
                            $"{Emoji.GlowingStar} Good news - multiple {Emoji.Airplane}{Emoji.Bed} flight and room options are available for *{HotelData.Name}*…\n" +
                            "\n" +
                            $"{Emoji.SpeechBalloon}Another recent review:\n" +
                            GetReviewSnipper(review2), out _);

                    }

                }

            }, CancellationToken);

        private async Task CheckCSWindowOpenOrWait()
        {
            while (!TwilioService.IsWhatsAppCSWindowOpen(PhoneNumber))
            {
                CancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(3), CancellationToken);
            }

            CancellationToken.ThrowIfCancellationRequested();
        }

        private static string GetRating(int rating) => string.Join("", Enumerable.Range(0, rating).Select(x => Emoji.Star));
        private static string GetReviewSnipper(HotelReview review)
            => $"{GetRating(review.Rating)} “_{review.Review}_” – {review.ReviewDateTime:d-MMM-yy}";

        public bool SendNudgeMessage()
        {
            if (!TwilioService.IsWhatsAppCSWindowOpen(PhoneNumber))
            {
                TwilioService.SendWhatsappMessage(PhoneNumber, string.Empty, out _, WhatsApp.AnyBetter.AknowledgeGenericTemplateSid);
                return false;
            }
            else
            {
                TwilioService.SendWhatsappMessage(PhoneNumber,
                    $"Just a reminder - your {HotelData.Name} results are ready {Emoji.WhiteHeavyCheckMark}\n" +
                    $"Open them here: {GetLinkForDealCheckRequest()}", out _);
                return true;
            }
        }

        public bool SendNudgeMessage_clarification()
        {

            if (!TwilioService.IsWhatsAppCSWindowOpen(PhoneNumber))
            {
                TwilioService.SendWhatsappMessage(PhoneNumber, string.Empty, out _, WhatsApp.AnyBetter.AknowledgeGenericTemplateSid);
                return false;
            }
            else
            {
                TwilioService.SendWhatsappMessage(PhoneNumber,
                    $"We need a little more info before we can start searching for your best deals {Emoji.LowerRightPencil}\n" +
                    $"It’ll only take a second - see what we need here {Emoji.WhiteRightPointingBackhandIndex}{Emoji.LeftPointingMagnifyingGlass}" +
                    $"{GetLinkForDealCheckRequest(false)}", out _);
                return true;
            }
        }
    }
}
