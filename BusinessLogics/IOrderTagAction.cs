namespace OrderApiFun.BusinessLogics
{
    /// <summary>
    /// 标签功能接口, 根据<see cref="OrderTag"/>标签的功能执行
    /// </summary>
    public interface IOrderTagAction
    {
        Task ExecuteAsync(int orderId, IDictionary<string, object> parameters);
    }

    /// <summary>
    /// 紧急标签的例子
    /// </summary>
    public class UrgentOrderTagAction : IOrderTagAction
    {
        public async Task ExecuteAsync(int orderId, IDictionary<string, object> parameters)
        {
            // 实现紧急订单的相关功能，例如发送通知、调整优先级等
            throw new NotImplementedException();
        }
    }
}