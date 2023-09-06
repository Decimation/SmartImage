using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using SmartImage.Lib;

namespace SmartImage.UI.Model
{
	public class SharedInfo : INotifyPropertyChanged
	{
		public SharedInfo()
		{
			// Client = new SearchClient(new SearchConfig());

			m_hydrus = new HydrusClient();
		}

		public readonly HydrusClient m_hydrus;

		private string       _hash;

		public string Hash
		{
			get { return _hash; }
			set
			{
				if (_hash != value) {
					_hash = value;
					OnPropertyChanged();
				}
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}