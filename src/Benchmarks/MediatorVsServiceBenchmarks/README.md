Mediator vs Service Benchmarks

Run the benchmarks (requires .NET 10 SDK):

```bash
cd src/Benchmarks/MediatorVsServiceBenchmarks
./run.sh
```

This project compares the repository's mediator implementation against a direct service interface implementation. Results are produced by BenchmarkDotNet and will be output to the `BenchmarkDotNet.Artifacts` folder.

