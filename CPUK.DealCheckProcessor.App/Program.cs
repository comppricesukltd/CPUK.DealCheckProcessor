using CPUK.DealCheckProcessor.App.Service;
using CPUK.Utility.Logging;
using CPUK.Utility.Logging.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace CPUK.DealCheckProcessor.App
{
    internal class Program
    {
        static async Task Main(string[] args)
            => await RemoteLogger.HandleEntryPointAsync(AppType.BatchJob, async () =>
            {

#if DEBUG
                //args = new[] { "-nudge" };
                args = new[] { "-competitor" };
                //UserMessagingService.TestMLMessage(7, 4, "447802739830");
                //UserMessagingService.TestMLMessage(2, 1, "380663345436");

                //await new DealCheckProcessingService().Test();
#endif
                if (args.ElementAtOrDefault(0) == "-competitor" || args.Length == 0)
                    await new DealCheckProcessingService().Run();

                if (args.ElementAtOrDefault(0) == "-nudge")
                    await new ResultsReviewNudgeService().Run();

                RemoteLogger.Default.FinalizeLog();
            },true);
    }

}
