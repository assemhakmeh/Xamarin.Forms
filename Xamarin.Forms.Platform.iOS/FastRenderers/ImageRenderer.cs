using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using Xamarin.Forms.Internals;
using RectangleF = CoreGraphics.CGRect;

namespace Xamarin.Forms.Platform.iOS.FastRenderers
{
	public static class ImageExtensions
	{
		public static UIViewContentMode ToUIViewContentMode(this Aspect aspect)
		{
			switch (aspect)
			{
				case Aspect.AspectFill:
					return UIViewContentMode.ScaleAspectFill;
				case Aspect.Fill:
					return UIViewContentMode.ScaleToFill;
				case Aspect.AspectFit:
				default:
					return UIViewContentMode.ScaleAspectFit;
			}
		}
	}

	public class ImageRenderer : UIImageView, IVisualElementRenderer
	{
		bool _isDisposed;

		readonly VisualElementRendererBridge _visualElementRendererBridge;

		public event EventHandler<VisualElementChangedEventArgs> ElementChanged;

		UIImageView Control => this as UIImageView;

		public Image Element { get; private set; }

		public UIView NativeView => this;

		UIViewController IVisualElementRenderer.ViewController => null;

		VisualElement IVisualElementRenderer.Element => Element;

		public ImageRenderer() : base(RectangleF.Empty)
		{
			_visualElementRendererBridge = new VisualElementRendererBridge(this);
		}


		protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				_visualElementRendererBridge?.Dispose();

				UIImage oldUIImage;
				if (Control != null && (oldUIImage = Control.Image) != null)
				{
					oldUIImage.Dispose();
				}
			}

			_isDisposed = true;

			base.Dispose(disposing);
		}

		void IVisualElementRenderer.SetElement(VisualElement element)
		{
			var oldElement = Element;
			Element = element as Image;

			if (oldElement != null)
				oldElement.PropertyChanged -= OnElementPropertyChanged;

			if (element != null)
			{
				element.PropertyChanged += OnElementPropertyChanged;
			}

			ElementChanged?.Invoke(this, new VisualElementChangedEventArgs(oldElement, element));

			if (element != null)
			{
				if (Control == null)
				{
					Control.ContentMode = UIViewContentMode.ScaleAspectFit;
					Control.ClipsToBounds = true;
				}

				SetAspect();
				/* await */
				TrySetImage(oldElement as Xamarin.Forms.Image).Wait();
				SetOpacity();
			}

		}

		void IVisualElementRenderer.SetElementSize(Size size)
		{
			Layout.LayoutChildIntoBoundingRegion(Element, new Rectangle(Element.X, Element.Y, size.Width, size.Height));
		}

		SizeRequest IVisualElementRenderer.GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			return Control.GetSizeRequest(widthConstraint, heightConstraint);
		}


		public override void LayoutSubviews()
		{
			base.LayoutSubviews();
			_visualElementRendererBridge.LayoutSubviews();
		}

		async void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == Xamarin.Forms.Image.SourceProperty.PropertyName)
				await TrySetImage();
			else if (e.PropertyName == Xamarin.Forms.Image.IsOpaqueProperty.PropertyName)
				SetOpacity();
			else if (e.PropertyName == Xamarin.Forms.Image.AspectProperty.PropertyName)
				SetAspect();
		}

		void SetAspect()
		{
			if (_isDisposed || Element == null || Control == null)
			{
				return;
			}

			Control.ContentMode = Element.Aspect.ToUIViewContentMode();
		}

		async Task TrySetImage(Image previous = null)
		{
			// By default we'll just catch and log any exceptions thrown by SetImage so they don't bring down
			// the application; a custom renderer can override this method and handle exceptions from
			// SetImage differently if it wants to

			try
			{
				await SetImage(previous).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.Warning(nameof(ImageRenderer), "Error loading image: {0}", ex);
			}
			finally
			{
				((IImageController)Element)?.SetIsLoading(false);
			}
		}

		async Task SetImage(Image oldElement = null)
		{
			if (_isDisposed || Element == null || Control == null)
			{
				return;
			}

			var source = Element.Source;

			if (oldElement != null)
			{
				var oldSource = oldElement.Source;
				if (Equals(oldSource, source))
					return;

				if (oldSource is FileImageSource && source is FileImageSource && ((FileImageSource)oldSource).File == ((FileImageSource)source).File)
					return;

				Control.Image = null;
			}

			IImageSourceHandler handler;

			Element.SetIsLoading(true);

			if (source != null &&
				(handler = Internals.Registrar.Registered.GetHandler<IImageSourceHandler>(source.GetType())) != null)
			{
				UIImage uiimage;
				try
				{
					uiimage = await handler.LoadImageAsync(source, scale: (float)UIScreen.MainScreen.Scale);
				}
				catch (OperationCanceledException)
				{
					uiimage = null;
				}

				if (_isDisposed)
					return;

				var imageView = Control;
				if (imageView != null)
					imageView.Image = uiimage;

				((IVisualElementController)Element).NativeSizeChanged();
			}
			else
			{
				Control.Image = null;
			}

			Element.SetIsLoading(false);
		}

		void SetOpacity()
		{
			if (_isDisposed || Element == null || Control == null)
			{
				return;
			}

			Control.Opaque = Element.IsOpaque;
			Control.ClearsContextBeforeDrawing = !Control.Opaque;
		}
	}

}