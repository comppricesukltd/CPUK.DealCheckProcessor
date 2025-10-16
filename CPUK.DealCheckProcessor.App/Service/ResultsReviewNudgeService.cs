using CPUK.BusinessLogic.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CPUK.DealCheckProcessor.App.Service
{
    public class ResultsReviewNudgeService
    {
        private readonly DealCheckService dealCheckService = new DealCheckService();


        public async Task Run()
        {
            var dealCheckListToNudge = dealCheckService.GetDealCheckRequestListToNudge();
            var dealCheckListToNudge_clarification = dealCheckService.GetDealCheckRequestListToNudge_Clarification();
            var semaphore = new SemaphoreSlim(5);
            var taskList = new List<Task>();
            foreach (var dealCheck in dealCheckListToNudge)
            {
                await semaphore.WaitAsync();
                taskList.Add(Task.Run(() =>
                {
                    try { new UserMessagingService(dealCheck).SendNudgeMessage("https://anybetter.com/results"); }
                    finally { semaphore.Release(); }
                }));
            }

            foreach (var dealCheck in dealCheckListToNudge_clarification)
            {
                await semaphore.WaitAsync();
                taskList.Add(Task.Run(() =>
                {
                    try { new UserMessagingService(dealCheck).SendNudgeMessage_clarification( ); }
                    finally { semaphore.Release(); }
                }));
            }

            await Task.WhenAll(taskList);
        }
    }
}
