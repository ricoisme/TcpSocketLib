using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Mathematics;

namespace RxSocket.Benchmarks
{
    [ShortRunJob]
    [RankColumn(NumeralSystem.Arabic)]
    public class MyBenchmarks
    {
        private readonly ServerConfig _config;
        private readonly IClientSocket _client;
        private readonly ISocketService _tcpService;

        [Params(100, 1000, 10000)]
        public int Totals { get; set; }

        [Params(1024, 4096, 8192)]
        public int BufferSize { get; set; }

        public MyBenchmarks()
        {
            _config = new ServerConfig
            {
                BufferSize = 10240,
                Backlog = 240,
                IpAddress = "127.0.0.1",
                Port = 8787,
                Retry = 3
            };
            _tcpService = new TcpService(_config);
            _client = new ClientSocket();
        }

        [GlobalSetup]
        public void Setup()
        {
            _tcpService.Accepted.SubscribeOn(TaskPoolScheduler.Default)
                .Subscribe
                (
                 r => _client.ConnectAsync(_config.IpAddress, _config.Port),
                 ex => Console.WriteLine(ex),
               () => Console.WriteLine("Server Start completed")
                );
            _tcpService.Reciever.SubscribeOn(TaskPoolScheduler.Default)
               .Subscribe(
               r => Console.WriteLine($"Receive:{r.Message} from [{r.EndPoint}]"),
               ex => Console.WriteLine(ex),
               () => Console.WriteLine("Socket receiver completed")
               );
            Task.Run(() => _tcpService.StartAsync());
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _client.Disconnect();
            _tcpService.Stop();
        }

        [Benchmark]
        [MemoryDiagnoser]
        public void SendAsyncMessage()
        {
            var line = Encoding.UTF8.GetBytes(new string('r', BufferSize) + Environment.NewLine);
            while (Totals > 0)
            {
                _client.SendAsync(line, 0).ConfigureAwait(false);
                Totals--;
            }
        }
    }
}
