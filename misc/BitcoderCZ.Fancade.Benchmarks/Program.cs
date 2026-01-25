using BenchmarkDotNet.Running;
using RhoMicro.BdnLogging;

BenchmarkRunner.Run(typeof(Program).Assembly, config: SpotlightConfig.Instance, args: args);