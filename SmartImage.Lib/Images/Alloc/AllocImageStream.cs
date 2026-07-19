using System;
using System.Collections.Generic;
using System.Text;

namespace SmartImage.Lib.Images.Alloc;

public class AllocImageStream : AllocImage
{

	public AllocImageStream(Stream value, UniImageType type) : base($"{value.ToString()}", type)
	{
		throw new NotImplementedException();

	}

	public override async Task<bool> AllocSourceAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

}