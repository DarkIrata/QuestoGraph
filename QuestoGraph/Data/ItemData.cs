using Lumina.Excel.Sheets;

namespace QuestoGraph.Data
{
    internal class ItemData
    {
        public uint RowId { get; }

        public uint Icon { get; }

        public string Name { get; }

        public uint Amount { get; }

        public bool IsHQ { get; }

        public bool HasStain => this.Stain != null && this.Stain.Value.RowId != 0;

        public Stain? Stain { get; private set; }

        public ItemData(uint rowId, uint icon, string name, uint amount, bool isHQ, Stain? stain)
        {
            this.RowId = rowId;
            this.Icon = icon;
            this.Name = name;
            this.Amount = amount;
            this.IsHQ = isHQ;
            this.Stain = stain;
        }
    }
}
