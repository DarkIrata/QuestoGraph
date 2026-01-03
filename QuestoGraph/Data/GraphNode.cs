namespace QuestoGraph.Data
{
    internal class GraphNode
    {
        public uint QuestId { get; set; } = 0;

        public string Name { get; set; }

        public QuestData? QuestData { get; set; }

        public GraphNode(string name)
        {
            this.Name = name;
        }

        public GraphNode(uint questId, string name)
            : this(name)
        {
            this.QuestId = questId;
        }

        public GraphNode(QuestData questData)
            : this(questData.RowId, questData.Name)
        {
            this.QuestData = questData;
        }
    }
}
