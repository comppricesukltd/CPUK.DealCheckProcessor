using CPUK.BusinessLogic.Services;
using CPUK.DataAccess;
using CPUK.DataAccess.Repositories;
using CPUK.Domain.Data;
using CPUK.Domain.Entities.Company;
using CPUK.Domain.Entities.DealCheck;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CPUK.DealCheckProcessor.App.Service
{

    public class DealCheckProcessingService
    {
        private readonly DealCheckService dealCheckService = new DealCheckService();
        private readonly DealCheckRepository dealCheckRepository = new DealCheckRepository();
        private readonly BusinessLogic.Services.DealCheckProcessingService dealCheckProcessingService = new BusinessLogic.Services.DealCheckProcessingService();
        public async Task Run()
        {
            var semaphore = new SemaphoreSlim(3);
            var taskList = new List<Task>();

            var dealCheckList = dealCheckRepository.GetDealCheckRequestListNotCompleted().Where(x => x.Criteria.IsComplete).ToList();

            foreach (var dealCheck in dealCheckList) taskList.Add(await ProduceCompetitors(dealCheck, semaphore));

            taskList = taskList.Where(x => x != null).ToList();

            if (taskList.Any()) await Task.WhenAll(taskList);
        }

        public async Task<Task> ProduceCompetitors(DealCheckRequestFull dealCheckRequest, SemaphoreSlim semaphore)
        {


            Task finalTask = null;
            var taskList = new List<Task<bool>>();
            try
            {

                var messagingService = new UserMessagingService(dealCheckRequest);
                messagingService.StartUserMessaging();


                foreach (var company in StaticDataHolder.Company)
                {
                    if (company.Id == CompanyId.BookingCom) continue;//skip booking.com temporarily

                    await semaphore.WaitAsync();
                    taskList.Add(TryProduceCompetitors(dealCheckRequest, company).ContinueWith(ct => { semaphore.Release(); return ct.Result; }));
                }

                finalTask = Task.Run(async () =>
                {
                    bool anyCompetitorSet = false;
                    try
                    {
                        if (anyCompetitorSet = (await Task.WhenAll(taskList)).Any())
                            dealCheckRepository.WriteDealCheckRequestCompleted(dealCheckRequest.Id);
                    }
                    catch { }


                    messagingService.StopUserMessaging();
                    if (anyCompetitorSet) messagingService.SendCompletedNotification();
                });
            }
            catch
            {

            }
            return finalTask;
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

    }
}
