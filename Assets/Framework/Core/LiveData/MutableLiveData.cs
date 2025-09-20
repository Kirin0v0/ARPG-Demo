namespace Framework.Core.LiveData
{
    /// <summary>
    /// 在继承LiveData的基础上实现可写入数据，用于可读可写的场景
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    public class MutableLiveData<T> : LiveData<T>
    {
        public MutableLiveData(LiveDataMode mode = LiveDataMode.Default): base(mode)
        {
        }

        public MutableLiveData(T value, LiveDataMode mode = LiveDataMode.Default) : base(value, mode)
        {
        }

        public new void SetValue(T value)
        {
            base.SetValue(value);
        }
    }
}