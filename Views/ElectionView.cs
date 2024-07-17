using System.Text.Json.Serialization;
using CsvHelper.Configuration.Attributes;

namespace RezultateVotDataExtractor.Views;

public class ElectionView
{
    public string ElectionName { get; set; }
    public string Category { get; set; }
    public string BallotName { get; set; }
    public int BallotId { get; set; }
    
    [JsonIgnore]
    [Ignore]
    public int BallotType { get; set; }
    public int ElectionId { get; set; }
    public DateTime Date { get; set; }
}