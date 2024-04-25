namespace RezultateVotDataExtractor.Views;

public class ElectionAttendanceInRomaniaView
{
    public int BallotId { get; set; }
    public int ElectionId { get; set; }
    public string BallotName { get; set; }
    public int Category { get; set; }
    public string ElectionName { get; set; }
    public string Subtitle { get; set; }
    public DateTime ElectionDate { get; set; }
    public string County { get; set; }
    public string Locality { get; set; }
    public int Round { get; set; }
    public DateTime BallotDate { get; set; }
    public int EligibleVoters { get; set; }
    public int TotalVotes { get; set; }
    public int ValidVotes { get; set; }
    public int NullVotes { get; set; }
}