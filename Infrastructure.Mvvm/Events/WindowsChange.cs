using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mvvm;


public class HeightChange : ValueChangedMessage<double>
{
    public HeightChange(double height) : base(height)
    {
    }
}
public class WidthChange : ValueChangedMessage<double>
{
    public WidthChange(double height) : base(height)
    {
    }
}
