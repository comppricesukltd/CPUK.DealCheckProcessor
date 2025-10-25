using CPUK.BusinessLogic.Services;
using CPUK.DataAccess;
using CPUK.DataAccess.Repositories;
using CPUK.Domain.Data;
using CPUK.Domain.Entities.Company;
using CPUK.Domain.Entities.DealCheck;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CPUK.DealCheckProcessor.App.Service
{

    public class DealCheckProcessingService
    {
        private readonly DealCheckService dealCheckService = new DealCheckService();
        private readonly DealCheckRepository dealCheckRepository = new DealCheckRepository();
        private readonly CompanyScriptRepository companyScriptRepository = new CompanyScriptRepository();
        private readonly BusinessLogic.Services.DealCheckProcessingService dealCheckProcessingService = new BusinessLogic.Services.DealCheckProcessingService();
        public async Task Run()
        {
            var scriptList = companyScriptRepository.GetCompanyScript();
            var screenshootScriptList = scriptList.Where(x => x.Type == CompanyScriptType.Screenshoot);

            var dealCheckList = dealCheckRepository.GetDealCheckRequestListNotCompleted();
            dealCheckList = dealCheckList.Where(x => x.Criteria.IsComplete).ToList();
            var semaphore = new SemaphoreSlim(3);
            var taskList = new List<Task>();

            foreach (var dealCheck in dealCheckList.Where(x => x.Criteria.IsComplete))
            {
                await semaphore.WaitAsync();
                taskList.Add(ProduceCompetitors(dealCheck).ContinueWith(ct => semaphore.Release()));
            }
            await Task.WhenAll(taskList);
        }

        public async Task ProduceCompetitors(DealCheckRequestFull dealCheckRequest)
        {



            try
            {

                var messagingService = new UserMessagingService(dealCheckRequest);
                messagingService.StartUserMessaging();

                var semaphore = new SemaphoreSlim(5);

                var taskList = new List<Task<bool>>();

                foreach (var company in StaticDataHolder.Company)
                {
                    if (company.Id == CompanyId.BookingCom) continue;//skip booking.com temporarily

                    await semaphore.WaitAsync();
                    taskList.Add(TryProduceCompetitors(dealCheckRequest, company).ContinueWith(ct => { semaphore.Release(); return ct.Result; }));
                }
                var anyCompetitorSet = (await Task.WhenAll(taskList)).Any();
                if (anyCompetitorSet)
                {
                    dealCheckRepository.WriteDealCheckRequestCompleted(dealCheckRequest.Id);
                }


                messagingService.StopUserMessaging();
                messagingService.SendCompletedNotification();
            }
            catch
            {

            }
        }


        private async Task<bool> TryProduceCompetitors(DealCheckRequestFull dealCheckRequest, Company company)
        {
            var IsCompetitorSet = false;
            var offersCount = 0;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Console.WriteLine($"Start[{dealCheckRequest.Id}][{company.Name}]");

                var offerList = await dealCheckProcessingService.GetOfferList(dealCheckRequest, company.Id);
                offersCount = offerList?.Count ?? 0;

                if (offerList?.Any() ?? false)
                {


                    IsCompetitorSet = true;

                    dealCheckService.WriteDealCheckOffer(dealCheckRequest.Id, company.Id, offerList);


                }
            }
            catch (Exception error)
            {


            }
            stopwatch.Stop();
            Console.WriteLine($"End[{dealCheckRequest.Id}][{company.Name}] Count={offersCount}; time={stopwatch.ElapsedMilliseconds}ms");
            return IsCompetitorSet;
        }

    }
}
