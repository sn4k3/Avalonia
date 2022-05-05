#nullable enable

using System;
using Android.Views;
using Android.Widget;

using Avalonia.Controls.Platform;
using Avalonia.Platform;

namespace Avalonia.Android.Platform
{
    internal class AndroidNativeControlHostImpl : INativeControlHostImpl
    {
        private const string AndroidDescriptor = "JavaHandle";
        private readonly AvaloniaView _avaloniaView;

        public AndroidNativeControlHostImpl(AvaloniaView avaloniaView)
        {
            _avaloniaView = avaloniaView;
        }

        public INativeControlHostDestroyableControlHandle CreateDefaultChild(IPlatformHandle parent)
        {
            return new DumpView(_avaloniaView);
        }

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(Func<IPlatformHandle, IPlatformHandle> create)
        {
            var holder = new DumpView(_avaloniaView);
            AndroidNativeControlAttachment? attachment = null;
            try
            {
                var child = create(holder);
                // It has to be assigned to the variable before property setter is called so we dispose it on exception
#pragma warning disable IDE0017 // Simplify object initialization
                attachment = new AndroidNativeControlAttachment(holder, child);
#pragma warning restore IDE0017 // Simplify object initialization
                attachment.AttachedTo = this;
                return attachment;
            }
            catch
            {
                attachment?.Dispose();
                holder?.Destroy();
                throw;
            }
        }

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(IPlatformHandle handle)
        {
            return new AndroidNativeControlAttachment(new DumpView(_avaloniaView), handle)
            {
                AttachedTo = this
            };
        }

        public bool IsCompatibleWith(IPlatformHandle handle) => handle.HandleDescriptor == AndroidDescriptor;

        private class DumpView : FrameLayout, INativeControlHostDestroyableControlHandle
        {
            public DumpView(ViewGroup parent) : base(parent.Context!)
            {
            }

            public string HandleDescriptor => AndroidDescriptor;

            public void Destroy() => Dispose();
        }

        class AndroidNativeControlAttachment : INativeControlHostControlTopLevelAttachment
        {
            private DumpView? _holder;
            private IPlatformHandle? _child;
            private View? _view;
            private AndroidNativeControlHostImpl? _attachedTo;

            public AndroidNativeControlAttachment(DumpView holder, IPlatformHandle child)
            {
                _holder = holder;
                _child = child;

                _view = Java.Lang.Object.GetObject<View>(child.Handle, global::Android.Runtime.JniHandleOwnership.DoNotTransfer);
                _holder.AddView(_view);
                _holder!.Visibility = ViewStates.Gone;
            }

            void CheckDisposed()
            {
                if (_holder == null)
                    throw new ObjectDisposedException(nameof(AndroidNativeControlAttachment));
            }

            public void Dispose()
            {
                if (_child != null)
                {
                    _holder?.RemoveView(_view);
                }
                _holder?.Dispose();
                _holder = null;
                _child = null;
                _attachedTo = null;
                _view?.Dispose();
                _view = null;
            }

            public INativeControlHostImpl? AttachedTo
            {
                get => _attachedTo;
                set
                {
                    CheckDisposed();

                    var oldAttachedTo = _attachedTo;
                    _attachedTo = (AndroidNativeControlHostImpl?)value;
                    if (_attachedTo == null)
                    {
                        oldAttachedTo?._avaloniaView.RemoveView(_holder);
                    }
                    else
                    {
                        _attachedTo._avaloniaView.AddView(_holder);
                    }
                }
            }

            public bool IsCompatibleWith(INativeControlHostImpl host) => host is AndroidNativeControlHostImpl;

            public void HideWithSize(Size size)
            {
                CheckDisposed();
                _holder!.Visibility = ViewStates.Gone;
            }

            public void ShowInBounds(Rect bounds)
            {
                CheckDisposed();
                if (_attachedTo == null)
                    throw new InvalidOperationException("The control isn't currently attached to a toplevel");

                bounds *= _attachedTo._avaloniaView.TopLevelImpl.RenderScaling;
                _holder!.Visibility = ViewStates.Visible;
                _holder.LayoutParameters = new FrameLayout.LayoutParams((int)bounds.Width, (int)bounds.Height)
                {
                    LeftMargin = (int)bounds.X,
                    TopMargin = (int)bounds.Y
                };
                _holder.RequestLayout();
            }
        }
    }
}
