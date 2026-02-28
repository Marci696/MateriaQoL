namespace MateriaQol.RetrieveAllMateriaFromGearPiece;

public record struct GearPieceNodeData
{
    public uint ItemId;
    public byte StartingMateriaCount;
    public byte CurrentMateriaCount;
    public RetrievalAttemptStatus Status;
}
