using CPUK.Domain.Entities.DealCheck;
using CPUK.Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPUK.DealCheckProcessor.App.Domain.HolidayExtras.CarPark
{


    public class HolidayExtras_CarParkResponse
    {
        public string code { get; set; }
        public string unprefixedCode { get; set; }
        public int price { get; set; }
        public string sales_currency { get; set; }
        public string name { get; set; }
        public List<Image> images { get; set; }
        public string brand_image { get; set; }
        public string mobile_image { get; set; }
        public string wallet_image { get; set; }
        public Videos videos { get; set; }
        public object discount { get; set; }
        public List<string> similar_products { get; set; }
        public string grouping_name { get; set; }
        public string upsell_upgrade { get; set; }
        public List<string> upsell_upgrades { get; set; }
        public List<object> upsell_products { get; set; }
        public string upsell_title { get; set; }
        public string upsell_text { get; set; }
        public List<object> ghost_upsell_products { get; set; }
        public string type { get; set; }
        public object gate_price { get; set; }
        public Score score { get; set; }
        public Cancellation cancellation { get; set; }
        public string schedule { get; set; }
        public Location location { get; set; }
        public bool is_refundable { get; set; }
        public List<object> equivalent_flex_products { get; set; }
        public bool is_cancellable { get; set; }
        public List<object> booking_terms_info { get; set; }
        public bool special_offer { get; set; }
        public bool on_airport { get; set; }
        public bool walking_distance_to_airport { get; set; }
        public string address { get; set; }
        public string postcode { get; set; }
        public bool car_parked_for_you { get; set; }
        public string introduction { get; set; }
        public bool meet_and_greet { get; set; }
        public object lead_time_cancellation { get; set; }
        public List<string> terminals { get; set; }
        public Transfers transfers { get; set; }
        public string transfers_summary { get; set; }
        public string transfers_tip { get; set; }
        public double distance_miles { get; set; }
        public string logo { get; set; }
        public List<string> selling_texts { get; set; }
        public Sellpoints sellpoints { get; set; }
        public string directions { get; set; }
        public string info_block { get; set; }
        public object sales_introduction { get; set; }
        public List<MapPin> map_pins { get; set; }
        public string what_3_words { get; set; }
        public string telephone { get; set; }
        public string fax { get; set; }
        public string features { get; set; }
        public string security_measures { get; set; }
        public string information { get; set; }
        public string disabled_facilities { get; set; }
        public string maximum_car_size { get; set; }
        public string insurance { get; set; }
        public string supplier { get; set; }
        public bool holiday_extras_group { get; set; }
        public string car_stored_at { get; set; }
        public bool park_mark { get; set; }
        public bool recommended { get; set; }
        public bool congestion_charge { get; set; }
        public object special_condition { get; set; }
        public DistanceToTerminals distance_to_terminals { get; set; }
        public bool unnamed { get; set; }
        public object tags { get; set; }
        public object cruise_lines { get; set; }
        public bool qr_code { get; set; }
        public bool qr_code_supplier_ref { get; set; }
        public object product_terms { get; set; }
        public List<string> alternative_product_code { get; set; }
        public bool accessible { get; set; }
        public string package_name { get; set; }
        public bool has_electric_charging { get; set; }
        public bool has_vehicle_tracking { get; set; }
        public bool ghost { get; set; }
        public object ghost_reason { get; set; }
        public bool can_amend_cant_cancel { get; set; }
        public bool park_and_stroll { get; set; }
        public bool park_and_ride { get; set; }
        public bool electric_charging_included { get; set; }
        public bool keep_keys { get; set; }
        public bool no_wait_guarantee { get; set; }
        public string product_authority { get; set; }
        public string booking_term { get; set; }
        public string static_desktop_map { get; set; }
        public string static_mobile_map { get; set; }
        public bool economy_parking { get; set; }
        public string brand_name { get; set; }
        public bool not_reservable { get; set; }
        public bool outside_ulez { get; set; }
        public bool late_return_cover_included { get; set; }
        public string arrival_procedures { get; set; }
        public string departure_procedures { get; set; }
        public List<Procedure> procedures { get; set; }
        public BookAt book_at { get; set; }
        public UpgradeAt upgrade_at { get; set; }
        public PayAt pay_at { get; set; }
        public FlowAt flow_at { get; set; }
        public Meta _meta { get; set; }
        public Currencies currencies { get; set; }

        public DealCheckExtrasParking Map() => new DealCheckExtrasParking
        {
            AirportCode = location?.code,
            Currency = sales_currency,
            Description = introduction,
            Price = price / 100.0,
            ProviderImage = brand_image,
            ProviderName = brand_name ?? product_authority,
            SiteScore = ParseHelper.AsDouble(score?.score, '.'),
            TerminalList = terminals,
            Title = name,
        };
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
    public class Agent
    {
        public string code { get; set; }
        public string name { get; set; }
        public string phone { get; set; }
        public string type { get; set; }
        public object defaultCurrency { get; set; }
        public string group { get; set; }
        public string brand { get; set; }
        public bool is_cashback { get; set; }
        public bool is_ppc { get; set; }
        public List<object> pretick_selected_upgrades_for { get; set; }
    }

    public class All
    {
        public string terminal { get; set; }
        public int distance { get; set; }
        public int duration { get; set; }
    }

    public class ApplePay
    {
        public object auth_session_id { get; set; }
    }

    public class BookAt
    {
        public string method { get; set; }
        public string path { get; set; }
        public Params @params { get; set; }
        public Validation validation { get; set; }
    }

    public class Booking
    {
        public string code { get; set; }
        public object total_price { get; set; }
        public List<object> upgrades { get; set; }
        public Params @params { get; set; }
    }

    public class Cancellation
    {
        public int fee { get; set; }
        public int upsellSortOrder { get; set; }
    }

    public class Card
    {
        public List<string> brands { get; set; }
        public bool require_security_code { get; set; }
    }

    public class CardToken
    {
        public object token { get; set; }
        public object expiry { get; set; }
        public object security_code { get; set; }
    }

    public class CardWithSecurityCode
    {
        public object card_holder { get; set; }
        public object number { get; set; }
        public object start { get; set; }
        public object expiry { get; set; }
        public object issue_num { get; set; }
        public object security_code { get; set; }
    }

    public class ChipsBooking
    {
        public object @ref { get; set; }
        public object last_four { get; set; }
        public object expiry { get; set; }
    }

    public class Closest
    {
        public string terminal { get; set; }
        public int distance { get; set; }
        public int duration { get; set; }
    }

    public class Content
    {
        public string directions { get; set; }
        public string address { get; set; }
        public string procedures { get; set; }
        public string transfers { get; set; }
    }

    public class Currencies
    {
        public GBP GBP { get; set; }
    }

    public class DistanceToTerminals
    {
        public List<All> all { get; set; }
        public Closest closest { get; set; }
    }

    public class FlowAt
    {
        public string method { get; set; }
        public string path { get; set; }
        public Params @params { get; set; }
    }

    public class GBP
    {
        public int price { get; set; }
        public int exchangeRate { get; set; }
    }

    public class Image
    {
        public string src { get; set; }
        public string alt { get; set; }
    }

    public class Location
    {
        public string code { get; set; }
        public string name { get; set; }
        public string regional_hotel_info { get; set; }
        public string type { get; set; }
        public string iata { get; set; }
        public List<string> busyPeriods { get; set; }
        public object isBusyPeriod { get; set; }
    }

    public class MapPin
    {
        public string name { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
        public string src { get; set; }
        public string code { get; set; }
        public string label { get; set; }
        public string type { get; set; }
        public string marker_name { get; set; }
        public string id { get; set; }
        public string terminal { get; set; }
    }

    public class Meta
    {
        public Card card { get; set; }
        public Paypal paypal { get; set; }
    }

    public class Meta3
    {
        public Location location { get; set; }
        public Agent agent { get; set; }
        public object flight { get; set; }
        public string currency { get; set; }
    }

    public class NumberOfPassengers
    {
        public List<int> options { get; set; }
    }

    public class Options
    {
        public CardWithSecurityCode card_with_security_code { get; set; }
        public ChipsBooking chips_booking { get; set; }
        public CardToken card_token { get; set; }
        public PayBookingByCheque pay_booking_by_cheque { get; set; }
        public ReserveAndPayLater reserve_and_pay_later { get; set; }
        public ApplePay apple_pay { get; set; }
        public Paypal paypal { get; set; }
    }

    public class Params
    {
        public string agent { get; set; }
        public Booking booking { get; set; }
        public Payment payment { get; set; }
        public string discount_code { get; set; }
        public int? basePrice { get; set; }
        public object email { get; set; }
        public object payment_card_expiry { get; set; }
        public object payment_card_issue_num { get; set; }
        public object payment_card_number { get; set; }
        public object payment_card_security_code { get; set; }
        public object payment_card_start { get; set; }
        public object use_payment_service { get; set; }
        public object currency { get; set; }
        public object ddds_id { get; set; }
        public object title { get; set; }
        public object first_name { get; set; }
        public object last_name { get; set; }
        public object mobile { get; set; }
        public object phone { get; set; }
        public object postcode { get; set; }
        public object out_flight { get; set; }
        public object return_flight { get; set; }
        public object transfer_destination { get; set; }
        public object number_of_passengers { get; set; }
        public object operator_initials { get; set; }
        public object marketing_email_opt_in { get; set; }
        public object newsletter_opt_in_message { get; set; }
        public string code { get; set; }
        public int price { get; set; }
        public string sales_currency { get; set; }
        public object registration { get; set; }
        public object car_make { get; set; }
        public object car_model { get; set; }
        public object car_colour { get; set; }
        public object cancellation_waiver { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public string location { get; set; }
        public string product_token { get; set; }
        public object out_terminal { get; set; }
        public object return_terminal { get; set; }
        public object terminal { get; set; }
        public object destination { get; set; }
        public object mobile_num { get; set; }
    }

    public class PayAt
    {
        public string method { get; set; }
        public string path { get; set; }
        public Params @params { get; set; }
        public Validation validation { get; set; }
    }

    public class PayBookingByCheque
    {
        public object cheque_name { get; set; }
    }

    public class Payment
    {
        public bool required { get; set; }
        public bool canUseVouchers { get; set; }
        public bool canUseReserveAndPay { get; set; }
        public Options options { get; set; }
        public Meta meta { get; set; }
        public object verification_code { get; set; }
    }

    public class Paypal
    {
        public object order_code { get; set; }
        public string client_id { get; set; }
    }

    public class Procedure
    {
        public string date { get; set; }
        public string meta { get; set; }
        public Content content { get; set; }
    }

    public class ReserveAndPayLater
    {
        public object cheque_name { get; set; }
        public object has_payment_failed { get; set; }
        public object is_APM_reservation { get; set; }
    }


    public class Score
    {
        public string num_reviews { get; set; }
        public string num_comments { get; set; }
        public string will_book_again_percentage { get; set; }
        public string score { get; set; }
    }

    public class Sellpoints
    {
        public string location { get; set; }
        public string terminal { get; set; }
        public string transfers { get; set; }
        public string parking { get; set; }
        public string security { get; set; }
    }

    public class Title
    {
        public List<string> options { get; set; }
    }

    public class Transfers
    {
        public object price { get; set; }
        public string travel_time { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public string frequency { get; set; }
        public object frequency_note { get; set; }
        public bool available_24_hours { get; set; }
        public bool included_in_price { get; set; }
        public string text { get; set; }
    }

    public class UpgradeAt
    {
        public string method { get; set; }
        public string path { get; set; }
        public Params @params { get; set; }
    }

    public class Validation
    {
        public Title title { get; set; }
        public NumberOfPassengers number_of_passengers { get; set; }
    }

    public class Videos
    {
        public List<string> youtube { get; set; }
    }


}
