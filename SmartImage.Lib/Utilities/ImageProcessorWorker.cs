using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace SmartImage.Lib.Utilities
{
	internal class ImageProcessorWorker : BackgroundService
	{

		#region Overrides of BackgroundService

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested) {
				
			}
			
		}

		#endregion

	}
}
