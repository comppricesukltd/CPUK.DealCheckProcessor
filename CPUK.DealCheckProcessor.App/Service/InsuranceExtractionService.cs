using CPUK.BusinessLogic.Services;
using CPUK.DataAccess.Repositories;
using CPUK.DataAccess.SharedDataStorage.Mongo.Common;
using CPUK.Domain.Entities.DealCheck;
using CPUK.Domain.Entities.Hotel;
using CPUK.Domain.Entities.MongoDocument;
using CPUK.Domain.Entities.Proxy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static CPUK.BusinessLogic.Services.CompanyScriptExecutor;
using static CPUK.ConfigurationSettings.Settings.DataBase.MongoDB;

namespace CPUK.DealCheckProcessor.App.Service
{

    public class InsuranceExtractionService
    {
        private readonly TTIHotelRepository ttiHotelRepository = new TTIHotelRepository();
        private readonly Dictionary<string, TTIHotel> ttiHotelCache = new Dictionary<string, TTIHotel>();
        private TTIHotel GetTtiHotel(string ttiCode)
        {
            if (!ttiHotelCache.ContainsKey(ttiCode)) ttiHotelCache[ttiCode] = ttiHotelRepository.GetHotels(ttiCode)?.FirstOrDefault();
            return ttiHotelCache[ttiCode];
        }
        public async Task<List<DealCheckInsurance>> ExtractInsuranceForCriteria(DealCheckCriteria criteria)
        {
            var hotel = GetTtiHotel(criteria.TtiCode);
            var @params = new Dictionary<string, string> {
                { "script", $"InsuranceComp.py" },
                { "ADULTS", $"{criteria.Adults}" },
                { "CHILDREN", string.Join(",", criteria.ChildAges)},
                { "DEP_DATE", $"{criteria.DepartureDate.Value:dd/MM/yyyy}"},
                { "RTN_DATE", $"{criteria.DepartureDate.Value.AddDays(criteria.Duration.Value):dd/MM/yyyy}"},
                { "COUNTRY", hotel.Country}
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
                DealCheckInsuranceCacheStore.Value.Set(new DealCheckInsuranceCacheRow(cacheKey, data));

                return data;
            }

        }

        private static async Task<List<DealCheckInsurance>> GetRealtimeData(Dictionary<string, string> @params)
        {
            var result = await ExecuteRemoteScriptFull("run", @params, Transformers.AsObject<List<DealCheckInsurance_RawRow>>);
            return result.Select(x =>
            {
                try { return x.Map(); }
                catch { return null; }
            }).Where(x => x != null).ToList();
        }

        private readonly Lazy<MongoDBRTCDataStorageExpireable<string, DealCheckInsuranceCacheRow>> DealCheckInsuranceCacheStore
            = new Lazy<MongoDBRTCDataStorageExpireable<string, DealCheckInsuranceCacheRow>>(() =>
            {
                var instance = new MongoDBRTCDataStorageExpireable<string, DealCheckInsuranceCacheRow>(MongoDBInstance.MAIN, "deal_check", "InsuranceCache");
                instance.CreateZeroTTLIndexIfNotExists();
                return instance;
            });


    }

    public class DealCheckInsuranceCacheRow : IExpirableMongoDocument<string>
    {
        public string Id { get; set; }
        public DateTime ExpireAt { get; set; }
        public List<DealCheckInsurance> Data { get; set; }
        public DealCheckInsuranceCacheRow() { }
        public DealCheckInsuranceCacheRow(string id, List<DealCheckInsurance> data)
        {
            Id = id;
            Data = data;
            ExpireAt = DateTime.UtcNow.AddDays(1);
        }
    }

    public class DealCheckInsurance_RawRow
    {
        public class DealCheckInsurance_RawRow_Category
        {
            [JsonProperty("Value")] public string Value { get; set; }
            [JsonProperty("Extra")] public string Extra { get; set; }

            public (float value, float extra) Map()
            {
                float val = 0;
                float ex = 0;
                if (Value != null)
                {
                    var valStr = Value.Replace("£", "").Replace("m", "000000").Trim();
                    float.TryParse(valStr, out val);
                }
                if (Extra != null)
                {
                    var exStr = Extra.Replace("Excess:", "").Replace("£", "").Trim();
                    float.TryParse(exStr, out ex);
                }
                return (val, ex);
            }
        }
        [JsonProperty("Company")] public string Company { get; set; }
        [JsonProperty("Total Price")] public string TotalPrice { get; set; }
        [JsonProperty("Max excess")] public string MaxExcess { get; set; }
        [JsonProperty("Medical")] public DealCheckInsurance_RawRow_Category Medical { get; set; }
        [JsonProperty("Baggage")] public DealCheckInsurance_RawRow_Category Baggage { get; set; }
        [JsonProperty("Cancellation")] public DealCheckInsurance_RawRow_Category Cancellation { get; set; }

        public DealCheckInsurance Map()
        {
            var result = new DealCheckInsurance { CompanyName = Company };

            if (float.TryParse(TotalPrice.Replace("£", "").Trim(), out float totalPrice)) result.TotalPrice = totalPrice;

            if (float.TryParse(MaxExcess.Replace("£", "").Trim(), out float maxExcess)) result.MaxExcess = maxExcess;

            (result.MedicalLimit, result.MedicalExcess) = Medical.Map();

            (result.BaggageLimit, result.BaggageExcess) = Baggage.Map();

            (result.CancellationLimit, result.CancellationExcess) = Cancellation.Map();

            return result;
        }

    }
}
