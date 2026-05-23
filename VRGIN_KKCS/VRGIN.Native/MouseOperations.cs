namespace VRGIN.Native;

public class MouseOperations
{
	public static void SetCursorPosition(int X, int Y)
	{
		WindowsInterop.SetCursorPos(X, Y);
	}

	public static void SetClientCursorPosition(int x, int y)
	{
		WindowsInterop.RECT clientRect = WindowManager.GetClientRect();
		WindowsInterop.SetCursorPos(x + clientRect.Left, y + clientRect.Top);
	}

	public static WindowsInterop.POINT GetClientCursorPosition()
	{
		WindowsInterop.POINT cursorPosition = GetCursorPosition();
		WindowsInterop.RECT clientRect = WindowManager.GetClientRect();
		return new WindowsInterop.POINT(cursorPosition.X - clientRect.Left, cursorPosition.Y - clientRect.Top);
	}

	public static void SetCursorPosition(WindowsInterop.POINT point)
	{
		WindowsInterop.SetCursorPos(point.X, point.Y);
	}

	public static WindowsInterop.POINT GetCursorPosition()
	{
		if (!WindowsInterop.GetCursorPos(out var lpMousePoint))
		{
			lpMousePoint = new WindowsInterop.POINT(0, 0);
		}
		return lpMousePoint;
	}

	public static void MouseEvent(WindowsInterop.MouseEventFlags value)
	{
		WindowsInterop.POINT cursorPosition = GetCursorPosition();
		WindowsInterop.mouse_event((int)value, cursorPosition.X, cursorPosition.Y, 0, 0);
	}
}
