namespace MergeBoard
{
    /// <summary>주문 템플릿의 요구 아이템·수량·보상을 보관한다.</summary>
    public sealed class OrderModel
    {
        public string Id { get; }
        public ItemStage RequiredStage { get; }
        public int RequiredCount { get; }
        public int Reward { get; }
        internal OrderModel(string id, ItemStage requiredStage, int requiredCount, int reward)
        {
            Id = id; RequiredStage = requiredStage; RequiredCount = requiredCount; Reward = reward;
        }
    }
}
