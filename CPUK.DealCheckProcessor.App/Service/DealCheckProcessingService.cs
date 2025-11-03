using CPUK.BusinessLogic.ApiClient.Local.OCRFromImage;
using CPUK.BusinessLogic.ApiClient.OpenAI.Concrete;
using CPUK.BusinessLogic.Services;
using CPUK.DataAccess;
using CPUK.DataAccess.Repositories;
using CPUK.Domain.Data;
using CPUK.Domain.DBModels.DealCheck;
using CPUK.Domain.Entities.Company;
using CPUK.Domain.Entities.DealCheck;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static CPUK.ConfigurationSettings.Settings;

namespace CPUK.DealCheckProcessor.App.Service
{

    public class DealCheckProcessingService
    {
        private readonly BusinessLogic.DealCheckProcessing.DealCheckProcessingService dealCheckProcessingService = new BusinessLogic.DealCheckProcessing.DealCheckProcessingService();

        private readonly TTIHotelMappingRepository ttiHotelMappingRepository = new TTIHotelMappingRepository();
        private readonly CompanyScriptService companyScriptService = new CompanyScriptService();
        private readonly DealCheckRepository dealCheckRepository = new DealCheckRepository();
        private readonly OCRFromImageService ocrFromImageService = new OCRFromImageService();
        private readonly DealCheckService dealCheckService = new DealCheckService();
        private readonly S3FileService s3FileService = new S3FileService();




        public async Task Run()
        {
            var ss_inner = new SemaphoreSlim(3);
            var ss_outter = new SemaphoreSlim(3);
            var taskList = new List<Task>();

            var dealCheckList = dealCheckRepository.GetDealCheckRequestListNotCompleted();


            foreach (var dealCheck in dealCheckList)
            {
                await ss_outter.WaitAsync();
                taskList.Add(Task.Run(async () =>
                {
                    try
                    {
                        if (dealCheck.Criteria == null) await ProduceCriteria(dealCheck, ss_inner);
                        if (dealCheck.Criteria?.IsComplete ?? false) await ProduceCompetitors(dealCheck, ss_inner);
                    }
                    finally { ss_outter.Release(); }
                }));
            }

            if (taskList.Any()) await Task.WhenAll(taskList);
        }

        private readonly OpenAIImageDealImageDetectionService OpenAIImageDealImageDetectionService = new OpenAIImageDealImageDetectionService();
        public async Task ProduceCriteria(DealCheckRequestFull request, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            request.Criteria = await ProduceCriteria(request);
            semaphore.Release();
            if (request.Criteria != null)
            {

                var result = new DealSubmitResult(request.Id, request.Criteria.Components);
                if (result.Result == DealCheckSubmitResult.Success)
                {
                    try
                    {

                        dealCheckRepository.WriteDealCheckCriteriaCompleted(request.Criteria.Id);
                        await dealCheckService.OnDealCheckRequestCreatedSuccessfully(request);
                    }
                    catch
                    {

                    }

                }
            }
            else
            {
                dealCheckRepository.WriteDealCheckRequestFailed(request.Id);
            }
        }
        public async Task<DealCheckCriteria> ProduceCriteria(DealCheckRequestFull request)
        {
            var company = StaticDataHolder.Company.FirstOrDefault(x => x.Id == request.CompanyId);
            try
            {
                if (!string.IsNullOrEmpty(request.Url))
                {
                    if (request.IsUrlValid ?? false)
                    {
                        return await ProduceCriteriaFromURL(request, company);
                    }
                    else
                    {
                        var (imageBytes, imageType) = await s3FileService.ReadObjectAsync(AWS_S3.DealCheckInputStore, $"input_image/{request.ImageId}");
                        var response = await OpenAIImageDealImageDetectionService.Detect(imageBytes, imageType);
                        if (response.isDeal && !(response.is404 || response.isOtherError))
                        {
                            return await ProduceCriteriaFromImage(request);
                        }
                        else
                        {
                            return await ProduceCriteriaFromImageNoExtractin(request, company);
                        }
                    }
                }
                else
                {
                    return await ProduceCriteriaFromImage(request);
                }

            }
            catch { }
            return null;
        }
        private async Task<DealCheckCriteria> ProduceCriteriaFromURL(DealCheckRequestFull request, Company company)
            => WriteDealcheckCriteria(request.Id, await dealCheckProcessingService.GetCriteria(company.Id, request.Url));

        private async Task<DealCheckCriteria> ProduceCriteriaFromImage(DealCheckRequestFull request)
        {
            var (imageBytes, _) = await s3FileService.ReadObjectAsync(AWS_S3.DealCheckInputStore, $"input_image/{request.ImageId}");
            var criteria = await ocrFromImageService.TryRecognizeDealCheckOffer(request.CompanyId.Value, imageBytes);

            if (criteria != null)
            {
                criteria.TtiCode = ttiHotelMappingRepository.TryGetTTICode(request.CompanyId.Value, criteria.ExtractedHotelName, criteria.ExtractedLocationName, out var ttiCode) ? ttiCode : null;
                WriteDealcheckCriteria(request.Id, criteria);
            }
            return criteria;
        }
        private async Task<DealCheckCriteria> ProduceCriteriaFromImageNoExtractin(DealCheckRequestFull request, Company company)
        {
            var imageId = await companyScriptService.GetScreenshootCached_TempStoreImageId(company.Id, $"https://www.{company.Domain}/");

            await s3FileService.CopyObjectAsync(AWS_S3.IntermediateStore, imageId, AWS_S3.DealCheckInputStore, $"input_image/{imageId}");
            return WriteDealcheckCriteria(request.Id, new DealCheckCriteria());

        }



        public DealCheckCriteria WriteDealcheckCriteria(int requestId, DealCheckCriteria criteria)
        {
            if (criteria != null)
            {
                var flightId = dealCheckService.WriteFlights(new[] { criteria.Flight }).FirstOrDefault().Value;
                criteria.Id = dealCheckRepository.CreateDealCheckRequestCriteria(new sp_create_deal_criteria_prc(requestId, flightId, criteria));
            }
            return criteria;

        }



        #region COMPETITORS
        public async Task ProduceCompetitors(DealCheckRequestFull dealCheckRequest, SemaphoreSlim semaphore)
        {


            var taskList = new List<Task<bool>>();
            try
            {

                var messagingService = new UserMessagingService(dealCheckRequest);
                messagingService.StartUserMessaging();


                foreach (var company in StaticDataHolder.Company)
                {
                    if (company.Id == CompanyId.BookingCom) continue;//booking.com go separately after all

                    await semaphore.WaitAsync();
                    taskList.Add(TryProduceCompetitors(dealCheckRequest, company).ContinueWith(ct => { semaphore.Release(); return ct.Result; }));
                }
                
                
                bool anyCompetitorSet = false;
                try { anyCompetitorSet = (await Task.WhenAll(taskList)).Any(); } catch { }

                if (anyCompetitorSet)
                {
                    //refresh to get offers built with other companies
                    dealCheckRequest = dealCheckService.GetDealCheckRequestFull(dealCheckRequest.UserId, dealCheckRequest.Id);

                    try
                    {
                        var bookingCom = StaticDataHolder.Company.FirstOrDefault(x => x.Id == CompanyId.BookingCom);
                        await semaphore.WaitAsync();
                        await TryProduceCompetitors(dealCheckRequest, bookingCom);
                    }
                    finally { semaphore.Release(); }

                    dealCheckRepository.WriteDealCheckRequestCompleted(dealCheckRequest.Id);
                }



                messagingService.StopUserMessaging();
                if (anyCompetitorSet) messagingService.SendCompletedNotification();
            }
            catch
            {

            }
        }

        private async Task<bool> TryProduceCompetitors(DealCheckRequestFull dealCheckRequest, Company company)
        {
            var IsCompetitorSet = false;
            var offersCount = 0;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                Console.WriteLine($"Start[{dealCheckRequest.Id}][{company.Name}]");

                var offerList = await dealCheckProcessingService.GetOfferList(dealCheckRequest, company.Id);
                offersCount = offerList?.Count ?? 0;

                if (IsCompetitorSet = offerList?.Any() ?? false)
                    dealCheckService.WriteDealCheckOffer(dealCheckRequest.Id, company.Id, offerList);
            }
            catch { }
            stopwatch.Stop();
            Console.WriteLine($"End[{dealCheckRequest.Id}][{company.Name}] Count={offersCount}; time={stopwatch.ElapsedMilliseconds}ms");
            return IsCompetitorSet;
        }
        #endregion


    }
}
