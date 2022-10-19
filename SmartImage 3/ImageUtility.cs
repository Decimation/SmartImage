using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using Kantan.Utilities;
using Novus.FileTypes;
using OpenCvSharp;

namespace SmartImage;

public static class ImageUtility
{
	public static SSIMResult CalculateSimilarity(Mat i1, Mat i2, int wh)
	{
		const double C1 = 6.5025, C2 = 58.5225;

		/***************************** INITS **********************************/
		MatType d = MatType.CV_32F;

		i1.ConvertTo(i1, d); // cannot calculate on one byte large values
		i2.ConvertTo(i2, d);

		var p1 = i1.Width * i1.Height;
		var p2 = i2.Width * i2.Height;

		if (p1 > p2) {

			var x = i1[new Rect(new Point(0, 0), i2.Size())];

			//x.SaveImage(@"C:\Users\Deci\Pictures\x.jpg");
			//i1 = i1.Resize(i2.Size());
			i1 = x;
		}

		Mat i22  = i2.Mul(i2); // I2^2
		Mat i12  = i1.Mul(i1); // I1^2
		Mat i1I2 = i1.Mul(i2); // I1 * I2

		/***********************PRELIMINARY COMPUTING ******************************/

		Mat mu1 = new(), mu2 = new(); //
		Cv2.GaussianBlur(i1, mu1, new Size(wh, wh), 1.5);
		Cv2.GaussianBlur(i2, mu2, new Size(wh, wh), 1.5);

		Mat mu12   = mu1.Mul(mu1);
		Mat mu22   = mu2.Mul(mu2);
		Mat mu1Mu2 = mu1.Mul(mu2);

		Mat sigma12 = new(), sigma22 = new(), sigma1_2 = new();

		Cv2.GaussianBlur(i12, sigma1_2, new Size(wh, wh), 1.5);
		sigma1_2 -= mu12;

		Cv2.GaussianBlur(i22, sigma22, new Size(wh, wh), 1.5);
		sigma22 -= mu22;

		Cv2.GaussianBlur(i1I2, sigma12, new Size(wh, wh), 1.5);
		sigma12 -= mu1Mu2;

		///////////////////////////////// FORMULA ////////////////////////////////

		Mat t1 = 2 * mu1Mu2 + C1;
		Mat t2 = 2 * sigma12 + C2;
		Mat t3 = t1.Mul(t2);

		t1 = mu12 + mu22 + C1;
		t2 = sigma1_2 + sigma22 + C2;
		t1 = t1.Mul(t2); // t1 =((mu1_2 + mu2_2 + C1).*(sigma1_2 + sigma2_2 + C2))

		var ssimMap = new Mat();
		Cv2.Divide(t3, t1, ssimMap); // ssim_map =  t3./t1;

		Scalar mssim = Cv2.Mean(ssimMap); // mssim = average of ssim map

		var result = new SSIMResult
		{
			diff  = ssimMap,
			mssim = mssim
		};

		return result;
	}

	public record SSIMResult
	{
		public double score
		{
			get { return (mssim.Val0 + mssim.Val1 + mssim.Val2) / 3; }
		}

		public Scalar mssim;
		public Mat    diff;
	}

	public static async Task<SSIMResult?> CalculateSimilarityAsync(Url u, Mat src)
	{
		using var stream = await u.AllowAnyHttpStatus().GetStreamAsync();

		if (stream is { }) {
			var types = await IFileTypeResolver.Default.ResolveAsync(stream);

			if (types is { }) {
				if (types.Any(b => b.IsType(FileType.MT_IMAGE))) {
					Debug.WriteLine($"binary {u}");

					using var mat = Mat.FromImageData(stream.ToByteArray());

					var score = ImageUtility.CalculateSimilarity(src, mat, 11);

					return score;

				}

			}
		}

		return default;
	}
}