namespace T4E.Domain
{
    public readonly struct GameDate
    {
        public readonly int Year;
        public readonly int Week;     // 1..52
        public readonly Weekday Day;
        public readonly DaySegment Segment;
        public GameDate(int year, int week, Weekday day, DaySegment seg) { Year=year; Week=week; Day=day; Segment=seg; }
        public override string ToString() => $"{Year}-W{Week:D2} {Day} {Segment}";
        public GameDate NextSegment()
        {
            var nextSeg = (int)Segment < 3 ? (DaySegment)((int)Segment+1) : DaySegment.Morning;
            var nextDay = Segment == DaySegment.Night ? NextDay(Day) : Day;
            var nextWeek = (Day == Weekday.Sunday && Segment == DaySegment.Night) ? Week+1 : Week;
            return new GameDate(Year, nextWeek, nextDay, nextSeg);
        }
        static Weekday NextDay(Weekday d) => d == Weekday.Sunday ? Weekday.Monday : (Weekday)((int)d+1);
    }
}
