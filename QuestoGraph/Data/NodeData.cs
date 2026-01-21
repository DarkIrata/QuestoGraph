namespace QuestoGraph.Data
{
    internal class NodeData
    {
        public uint Id { get; set; } = 0;

        public string Text { get; set; }

        public QuestData? QuestData { get; set; }

        public NodeData(string name)
        {
            this.Text = name;
        }

        public NodeData(uint id, string name)
            : this(name)
        {
            this.Id = id;
        }

        public NodeData(QuestData questData)
            : this(questData.RowId, questData.Name)
        {
            this.QuestData = questData;
        }
    }
}
