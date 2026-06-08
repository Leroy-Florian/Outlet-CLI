namespace Outlet.Registry.Llm;

/// <summary>Token accounting for a completion, when the provider reports it.</summary>
public sealed record ChatUsage(int InputTokens, int OutputTokens, int TotalTokens);
