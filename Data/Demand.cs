﻿namespace TrustGuard.Data
{
    public class Demand
    {
        public int Id { get; set; }
        public DateTime IssuedAt { get; set; }
        public bool? IsSeen { get; set; }
        public DateTime? SolvedAt { get; set; }
        public int TotalSeconds { get; set; }
    }
}
