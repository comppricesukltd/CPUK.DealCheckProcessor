using CPUK.Authorization.Service;
using CPUK.BusinessLogic.Services;
using CPUK.DataAccess;
using CPUK.DataAccess.Repositories;
using CPUK.Domain.DBModels.DealCheck;
using CPUK.Domain.Entities.Company;
using CPUK.Domain.Entities.DealCheck;
using CPUK.Domain.Entities.Util;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace CPUK.DealCheckProcessor.App.Service
{

    public class DealCheckProcessingService
    {
        private readonly DealCheckService dealCheckService = new DealCheckService();
        private readonly DealCheckRepository dealCheckRepository = new DealCheckRepository();
        private readonly CompanyScriptRepository companyScriptRepository = new CompanyScriptRepository();
        private readonly CompanyScriptService companyScriptService = new CompanyScriptService();
        public async Task Run()
        {
            var scriptList = companyScriptRepository.GetCompanyScript();
            var screenshootScriptList = scriptList.Where(x => x.Type == CompanyScriptType.Screenshoot);

            var dealCheckList = dealCheckRepository.GetDealCheckRequestListNotCompleted();
            dealCheckList = dealCheckList.Where(x => x.Main.Offer.IsComplete).ToList();
            var semaphore = new SemaphoreSlim(3);
            var taskList = new List<Task>();

            foreach (var dealCheck in dealCheckList.Where(x => x.Main.Offer.IsComplete))
            {
                await semaphore.WaitAsync();
                taskList.Add(ProduceCompetitors(dealCheck).ContinueWith(ct => semaphore.Release()));
            }
            await Task.WhenAll(taskList);
        }

        public async Task ProduceCompetitors(DealCheckRequestFull dealCheckRequest)
        {




            var messagingService = new UserMessagingService(dealCheckRequest);
            messagingService.StartUserMessaging();

            var semaphore = new SemaphoreSlim(5);

            var taskList = new List<Task<bool>>();

            foreach (var company in StaticDataHolder.Company)
            {
                await semaphore.WaitAsync();
                taskList.Add(TryProduceCompetitors(dealCheckRequest, company).ContinueWith(ct => { semaphore.Release(); return ct.Result; }));
            }
            var anyCompetitorSet = (await Task.WhenAll(taskList)).Any();
            if (anyCompetitorSet)
            {
                dealCheckRepository.WriteDealCheckRequestCompleted(dealCheckRequest.Id);
            }


            messagingService.StopUserMessaging();
            messagingService.SendCompletedNotification("https://anybetter.com/results");
        }


        private async Task<bool> TryProduceCompetitors(DealCheckRequestFull dealCheckRequest, Company company)
        {
            var IsCompetitorSet = false;
            var offersCount = 0;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Console.WriteLine($"Start[{dealCheckRequest.Id}][{company.Name}]");

                var competitorsResponse = await companyScriptService.TryGetDealCheckOffer_Competitors(company.Id, dealCheckRequest.Main.Offer);
                offersCount = competitorsResponse?.OfferList?.Count ?? 0;

                if (competitorsResponse?.OfferList?.Any() ?? false)
                {

                    var dealCheckId = dealCheckRepository.CreateDealCheck(new sp_create_deal_check_prc
                    {

                        isMain = false,
                        url = competitorsResponse?.Url,
                        isUrlValid = true,
                        requestId = dealCheckRequest.Id,
                        companyId = company.Id, 
                    });

                    IsCompetitorSet = true;

                    dealCheckService.WriteDealCheckOffer(dealCheckId, competitorsResponse.OfferList);


                }
            }
            catch { }
            stopwatch.Stop();
            Console.WriteLine($"End[{dealCheckRequest.Id}][{company.Name}] Count={offersCount}; time={stopwatch.ElapsedMilliseconds}ms");
            return IsCompetitorSet;
        }

    }
}
