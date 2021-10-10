﻿using System;
using System.Threading.Tasks;
using OpenCvSharp;
using SmartImage.Lib;
using SmartImage.Lib.Utilities;
using Range = System.Range;

#pragma warning disable IDE0079
#pragma warning disable CS0168, CS1998
#pragma warning disable IDE0060

namespace Test
{
	/* 
	 * >>> SmartImage.Lib <<<
	 *
	 *
	 * - SearchClient is used to run advanced searches and utilizes multiple engines
	 * - Individual engines can also be used
	 *
	 */


	public static class Program
	{
		public static void OnResult(object _, ResultCompletedEventArgs e)
		{

			if (e.Result.IsSuccessful) {
				Console.WriteLine(e.Result);
			}
		}

		public static async Task Main(string[] args)
		{

			/*var q      = new ImageQuery(@"C:\Users\Deci\Pictures\Test Images\Test6.jpg");
			var engine = new SauceNaoEngine() { };
			engine.Authentication = "362e7e82bc8cf7f6025431fbf3006510057298c3";
			var task = engine.GetResultAsync(q);

			var engine2 = new IqdbEngine();
			var task2   = engine2.GetResultAsync(q);

			var tasks = new[] { task, task2 };
			Task.WaitAny(tasks);

			Console.WriteLine("waiting");
			var result = await task;


			Console.WriteLine(">> {0}", result);
			var result2 = await task2;
			Console.WriteLine(">> {0}", result2);*/

			/*const double C1 = 6.5025, C2 = 58.5225;
			/***************************** INITS *********************************#1#
			MatType d = MatType.CV_32F;

			Mat i1 = new(@"C:\Users\Deci\Pictures\b.jpg"), i2 = new(@"C:\Users\Deci\Pictures\c.jpg");
			i1.ConvertTo(i1, d); // cannot calculate on one byte large values
			i2.ConvertTo(i2, d);

			var p1 = i1.Width * i1.Height;
			var p2 = i2.Width * i2.Height;

			if (p1> p2) {


				var x        =i1[new Rect(new Point(0,0), i2.Size())];

				//x.SaveImage(@"C:\Users\Deci\Pictures\x.jpg");
				//i1 = i1.Resize(i2.Size());
				i1 = x;
			}

			Console.WriteLine(i1.Size());
			Console.WriteLine(i2.Size());

			Mat i22  = i2.Mul(i2); // I2^2
			Mat i12  = i1.Mul(i1); // I1^2
			Mat i1I2 = i1.Mul(i2); // I1 * I2

			/***********************PRELIMINARY COMPUTING *****************************#1#

			Mat mu1 = new(), mu2 = new(); //
			Cv2.GaussianBlur(i1, mu1, new Size(11, 11), 1.5);
			Cv2.GaussianBlur(i2, mu2, new Size(11, 11), 1.5);

			Mat mu12   = mu1.Mul(mu1);
			Mat mu22   = mu2.Mul(mu2);
			Mat mu1Mu2 = mu1.Mul(mu2);

			Mat sigma12 = new(), sigma22 = new(), sigma1_2 = new();

			Cv2.GaussianBlur(i12, sigma1_2, new Size(11, 11), 1.5);
			sigma1_2 -= mu12;

			Cv2.GaussianBlur(i22, sigma22, new Size(11, 11), 1.5);
			sigma22 -= mu22;

			Cv2.GaussianBlur(i1I2, sigma12, new Size(11, 11), 1.5);
			sigma12 -= mu1Mu2;

			///////////////////////////////// FORMULA ////////////////////////////////

			Mat t1 = 2 * mu1Mu2 + C1;
			Mat t2 = 2 * sigma12 + C2;
			Mat t3 = t1.Mul(t2);

			t1 = mu12 + mu22 + C1;
			t2 = sigma1_2 + sigma22 + C2;
			t1 = t1.Mul(t2); // t1 =((mu1_2 + mu2_2 + C1).*(sigma1_2 + sigma2_2 + C2))

			Mat ssimMap = new Mat();
			Cv2.Divide(t3, t1, ssimMap); // ssim_map =  t3./t1;

			Scalar mssim = Cv2.Mean(ssimMap); // mssim = average of ssim map


			SSIMResult result = new SSIMResult();
			result.diff  = ssimMap;
			result.mssim = mssim;
			Console.WriteLine(result.score);*/


			foreach (var v in await ImageHelper.FindDirectImages("https://danbooru.donmai.us/posts/3567935")) {
				Console.WriteLine(v);
			}

			
		}

		/*public class SSIMResult
		{
			public double score
			{
				get { return (mssim.Val0 + mssim.Val1 + mssim.Val2) / 3; }
			}

			public Scalar mssim;
			public Mat    diff;
		}*/
	}
}