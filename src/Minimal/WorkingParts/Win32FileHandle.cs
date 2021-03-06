namespace Minimal
{
    using System.Security.Permissions;
    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// Provides a class for Win32 safe handle implementations
    /// </summary>
    internal sealed class Win32FileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the Win32ApiFileHandle class, specifying whether the handle is to be reliably released.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal Win32FileHandle()
            : base(true)
        {
        }

        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        protected override bool ReleaseHandle()
        {
            if (!(IsInvalid || IsClosed))
            {
                return Win32SafeNativeMethods.FindClose(this);
            }
            return (IsInvalid || IsClosed);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the Win32ApiFileHandle class specifying whether to perform a normal dispose operation. 
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!(IsInvalid || IsClosed))
            {
                Win32SafeNativeMethods.FindClose(this);
            }
            base.Dispose(disposing);
        }
    }
}