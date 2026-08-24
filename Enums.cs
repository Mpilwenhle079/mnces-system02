namespace MnceShisanyama.Api.Models;

/// <summary>
/// Lifecycle of an order as it moves through payment and the kitchen workflow.
/// AwaitingPayment -> order captured from the cart but not yet paid; NOT visible
///                     on staff dashboards or counted in reporting.
/// PaymentConfirmed -> card/cash payment succeeded, but the customer hasn't tapped
///                     "Submit Order to Kitchen" yet. Still NOT visible to staff -
///                     this is the explicit confirmation step before the kitchen sees it.
/// Pending         -> customer submitted the paid order; kitchen now sees it and can start cooking.
/// Preparing       -> kitchen staff has accepted and is actively cooking it.
/// Ready           -> food is ready for collection / handover to a driver.
/// Completed       -> customer has received the order (collected / delivered / served).
/// Cancelled       -> order was cancelled by staff or customer, or payment failed permanently.
/// </summary>
public enum OrderStatus
{
    AwaitingPayment = 0,
    PaymentConfirmed = 1,
    Pending = 2,
    Preparing = 3,
    Ready = 4,
    Completed = 5,
    Cancelled = 6
}

/// <summary>
/// How the customer wants to receive the order.
/// </summary>
public enum OrderChannel
{
    Collection = 0,
    Delivery = 1,
    DineIn = 2
}

/// <summary>
/// Role assigned to a staff account, used to gate which dashboards/actions
/// a staff member can access.
/// </summary>
public enum StaffRole
{
    Kitchen = 0,
    Admin = 1
}

/// <summary>How the customer chose to pay.</summary>
public enum PaymentMethod
{
    Card = 0,
    CashOnCollection = 1
}

/// <summary>Card network, detected from the card number for display/reporting.</summary>
public enum CardBrand
{
    Unknown = 0,
    Visa = 1,
    Mastercard = 2,
    Amex = 3,
    Discover = 4,
    Diners = 5
}

/// <summary>Outcome of a payment attempt.</summary>
public enum PaymentStatus
{
    Succeeded = 0,
    Failed = 1
}

/// <summary>What kind of call the front-of-house/admin team is logging.</summary>
public enum CallCategory
{
    OrderIssue = 0,
    MissingItem = 1,
    DeliveryIssue = 2,
    Complaint = 3,
    GeneralInquiry = 4
}

/// <summary>Whether a logged call/issue has been dealt with.</summary>
public enum CallStatus
{
    Open = 0,
    Resolved = 1
}