using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Mvvm.Events;

public class WindowsLoading : ValueChangedMessage<bool>
{
	public WindowsLoading(bool loading) : base(loading)
	{

	}
}
