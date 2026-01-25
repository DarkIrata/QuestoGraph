namespace QuestoGraph.Services.Events
{
    internal interface IEvent
    {
        bool IsHandled { get; set; }
    }
}
