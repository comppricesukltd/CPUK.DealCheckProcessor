using CPUK.DataAccess;
using CPUK.DataAccess.SharedDataStorage.Mongo.Common;
using CPUK.DealCheckProcessor.App.Domain.HolidayExtras.CarPark;
using CPUK.Domain.Entities.Company;
using CPUK.Domain.Entities.DealCheck;
using CPUK.Domain.Entities.MongoDocument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CPUK.BusinessLogic.Services.CompanyScriptExecutor;
using static CPUK.ConfigurationSettings.Settings.DataBase.MongoDB;

namespace CPUK.DealCheckProcessor.App.Service
{
    public class ParkingExtrasExtractionService
    {
        private readonly Lazy<MongoDBRTCDataStorageExpireable<string, DealCheckExtrasParkingCacheRow>> DealCheckInsuranceCacheStore
            = new Lazy<MongoDBRTCDataStorageExpireable<string, DealCheckExtrasParkingCacheRow>>(() =>
            {
                var instance = new MongoDBRTCDataStorageExpireable<string, DealCheckExtrasParkingCacheRow>(MongoDBInstance.MAIN, "deal_check", "ParkingExtrasCache");
                instance.CreateZeroTTLIndexIfNotExists();
                return instance;
            });

        public async Task<List<DealCheckExtrasParking>> ExtractParkingForCriteria(DealCheckCriteria criteria)
        {
            var londonOrigin = StaticDataHolder.OriginList.FirstOrDefault(x => x.Flightcode == "LON");
            var londonOriginList = StaticDataHolder.OriginList.Where(x => x.ParentId == londonOrigin.Id).ToList();
            if (criteria.OriginId == londonOrigin.Id)
            {
                var tasks = londonOriginList.Select(x => ExtractParkingForCriteria_ExactAirport(criteria, x.Flightcode)).ToList();
                var results = await Task.WhenAll(tasks);
                return results.SelectMany(x => x).ToList();
            }
            else
            {
                var origin = StaticDataHolder.OriginList.FirstOrDefault(x => x.Id == criteria.OriginId);
                if (origin == null) return new List<DealCheckExtrasParking>();
                return await ExtractParkingForCriteria_ExactAirport(criteria, origin.Flightcode);

            }
        }

        public async Task<List<DealCheckExtrasParking>> ExtractParkingForCriteria_ExactAirport(DealCheckCriteria criteria, string airport)
        {
            try
            {

                //https://www.holidayextras.com/static/?selectProduct=cp&#/carpark?lang=en&adults=2&depart=LGW&terminal=&arrive=&flight=&in=2025-12-26&out=2025-12-19&park_from=12%3A00%3A00&park_to=13%3A00&children=0&infants=0&from_categories=true
                var urlParams = new Dictionary<string, string>()
            {
                { "lang", "en" },
                { "adults", $"{criteria.Adults}" },
                { "depart", airport },
                { "terminal", "" },
                { "arrive", "" },
                { "flight", "" },
                { "out", $"{criteria.DepartureDate.Value:yyyy-MM-dd}" },
                { "in", $"{criteria.DepartureDate.Value.AddDays(criteria.Duration.Value):yyyy-MM-dd}" },
                { "park_from", "12:00:00" },
                { "park_to", "13:00" },
                { "children", $"{criteria.ChildAges?.Length ?? 0}" },
                { "infants", "0" },
                { "from_categories", "true" },

            };
                var urlQuery = string.Join("&", urlParams.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
                var url = $"https://www.holidayextras.com/static/?selectProduct=cp&#/carpark?{urlQuery}";
                var @params = new Dictionary<string, string> {
                { "script", $"holidayextras.py" },
                { "URL", url },
            };
                var cacheKey = string.Join("_", @params.Select(x => $"{x.Key}:{x.Value}"));

                var cahceRecord = DealCheckInsuranceCacheStore.Value.Get(cacheKey);
                if (cahceRecord?.Data?.Any() ?? false)
                {
                    return cahceRecord.Data;
                }
                else
                {
                    var data = await GetRealtimeData(@params);
                    DealCheckInsuranceCacheStore.Value.Set(new DealCheckExtrasParkingCacheRow(cacheKey, data));

                    return data;
                }
            }
            catch
            {
                return null;
            }
        }
        private static async Task<List<DealCheckExtrasParking>> GetRealtimeData(Dictionary<string, string> @params)
        {
            var (response, _) = await ExecuteRemoteScriptFull("run", @params, Transformers.AsObject<RemoteScriptRunnerResponse<List<HolidayExtras_CarParkResponse>>>);
            return response.data.Select(x =>
            {
                try { return x.Map(); }
                catch { return null; }
            }).Where(x => x != null).ToList();
        }
    }

    public class DealCheckExtrasParkingCacheRow : IExpirableMongoDocument<string>
    {
        public string Id { get; set; }
        public DateTime ExpireAt { get; set; }
        public List<DealCheckExtrasParking> Data { get; set; }
        public DealCheckExtrasParkingCacheRow() { }
        public DealCheckExtrasParkingCacheRow(string id, List<DealCheckExtrasParking> data)
        {
            Id = id;
            Data = data;
            ExpireAt = DateTime.UtcNow.AddDays(1);
        }
    }
}
