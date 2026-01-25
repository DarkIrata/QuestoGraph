using QuestoGraph.Data;

namespace QuestoGraph.Services.Events
{
    internal class GraphQuestClicked : IEvent
    {
        public bool IsHandled { get; set; }

        public QuestData Quest { get; }

        public GraphQuestClicked(QuestData quest)
        {
            this.Quest = quest;
        }
    }
}
