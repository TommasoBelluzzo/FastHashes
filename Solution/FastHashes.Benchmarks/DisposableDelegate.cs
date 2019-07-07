#region Using Directives
using System;
#endregion

namespace FastHashes.Benchmarks
{
    public sealed class DisposableDelegate : IDisposable
    {
        #region Members
        private Boolean m_IsDisposed;
        private readonly Action m_Disposer;
        #endregion

        #region Constructors
        public DisposableDelegate(Action initializer, Action disposer)
        {
            m_Disposer = disposer;
            initializer?.Invoke();
        }
        #endregion

        #region Destructors
        ~DisposableDelegate()
        {
            Dispose(false);
        }
        #endregion

        #region Methods
        private void Dispose(Boolean disposing)
        {
            if (m_IsDisposed)
                return;

            if (disposing)
                m_Disposer?.Invoke();

            m_IsDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);  
        }
        #endregion
    }
}