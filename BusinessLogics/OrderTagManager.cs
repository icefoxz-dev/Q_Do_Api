namespace OrderApiFun.BusinessLogics
{
    /// <summary>
    /// 订单标签管理器, 主要处理标签与功能的映射关系
    /// </summary>
    public class OrderTagManager
    {
        public IOrderTagAction GetActionForTag(string tagName)
        {
            switch (tagName)
            {
                case "Urgent":
                    return new UrgentOrderTagAction();
                // 添加其他标签与功能映射关系
                default:
                    return null;
            }
        }
    }
}