namespace RezultateVotDataExtractor.Views;

public class PartiesResultInRomaniaView
{
    public int ElectionId { get; set; }
    public int BallotId { get; set; }
    public string County { get; set; }
    public string Locality { get; set; }
    public string PartyName { get; set; }
    public string ShortName { get; set; }
    public string Alias { get; set; }
    public int? Votes { get; set; }
    public int? SeatsGained { get; set; }
    public int? TotalSeats { get; set; }
    public int? Seats1 { get; set; }
    public int? Seats2 { get; set; }
    public bool OverElectoralThreshold { get; set; }
}