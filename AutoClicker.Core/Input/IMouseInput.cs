using System.Drawing;

namespace AutoClicker.Core.Input;

public interface IMouseInput
{
    void Click(ClickButton button, ClickAction action, Point? fixedPosition);
}
