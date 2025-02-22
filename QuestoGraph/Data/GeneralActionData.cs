using Lumina.Excel.Sheets;

namespace QuestoGraph.Data
{
    internal class GeneralActionData
    {
        public GeneralAction GeneralAction { get; }

        public uint RowId => this.GeneralAction.RowId;

        public int Icon => this.GeneralAction.Icon;

        public string Name => this.GeneralAction.Name.ExtractText();

        public GeneralActionData(GeneralAction generalAction)
        {
            this.GeneralAction = generalAction;
        }
    }
}
