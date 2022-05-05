using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.Views;
using Android.Webkit;

using Avalonia.Controls;
using Avalonia.Platform;

namespace NativeEmbedSample.Android
{
    internal class EmbedSample : NativeControlHost
    {
        private View? _view;



        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            var webView = new WebView(global::Android.App.Application.Context);
            webView.LoadUrl("https://google.com");
            _view = webView;
            // _view = new global::Android.Widget.Button(global::Android.App.Application.Context) { Text = "Android button" };

            return new PlatformHandle(_view.Handle, "JavaHandle");
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            _view?.Dispose();
            _view = null;
            base.DestroyNativeControlCore(control);
        }
    }
}
