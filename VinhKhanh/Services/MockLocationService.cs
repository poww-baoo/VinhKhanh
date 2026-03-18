namespace VinhKhanh.Services;

public class MockLocationService
{
    public event EventHandler<(double Lat, double Lng)>?
        LocationChanged;

    private readonly (double Lat, double Lng)[] _route =
    [
        (10.75790, 106.68540),
        (10.75800, 106.68550),
        (10.75815, 106.68565),
        (10.75830, 106.68580),
        (10.75860, 106.68610),
        (10.75890, 106.68640),
        (10.75950, 106.68700),
        (10.75980, 106.68730),
        (10.76010, 106.68760),
        (10.76070, 106.68820),
    ];

    private int _step = 0;
    private System.Timers.Timer? _timer;
    private bool _running = false;

    public void Start()
    {
        if (_running) return;
        _running = true;
        _timer = new System.Timers.Timer(5000);
        _timer.Elapsed += (_, _) =>
        {
            var loc = _route[_step % _route.Length];
            _step++;
            LocationChanged?.Invoke(this, loc);
        };
        _timer.Start();
    }

    public void Stop()
    {
        _running = false;
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }
}