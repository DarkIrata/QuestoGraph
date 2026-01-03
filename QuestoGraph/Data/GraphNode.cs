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

        public GraphNode(QuestData questData)
        {
            this.QuestId = questData.RowId;
            this.Name = questData.Name;
            this.QuestData = questData;
        }
    }
}
