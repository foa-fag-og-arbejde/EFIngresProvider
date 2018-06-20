using System;
using System.Threading;
using System.Globalization;

namespace EFIngresProvider.Helpers
{
    public class CultureReestablisher : IDisposable
    {
        public CultureReestablisher()
        {
            _currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }

        private CultureInfo _currentCulture;

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = _currentCulture;
        }
    }
}
