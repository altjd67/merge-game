namespace MergeBoard
{
    /// <summary>고정된 주문 요구사항과 완료 여부를 보관한다.</summary>
    public sealed class OrderModel
    {
        public string Id { get; }
        public ItemStage RequiredStage { get; }
        public int RequiredCount { get; }
        public int Reward { get; }
        public bool Completed { get; internal set; }

        internal OrderModel(string id, ItemStage requiredStage, int requiredCount, int reward)
        {
            Id = id; RequiredStage = requiredStage; RequiredCount = requiredCount; Reward = reward;
        }
    }
}
