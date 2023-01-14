using System;

namespace Command.Utils; 

public class DisposableWrapper : IDisposable {
    private readonly Action _down;

    public DisposableWrapper(Action up, Action down) {
        this._down = down;

        up();
    }

    public void Dispose() {
        this._down();

        GC.SuppressFinalize(this);
    }
}