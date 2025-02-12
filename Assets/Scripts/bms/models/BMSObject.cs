using System.Collections.Generic;

namespace BMS
{
	public class Line
	{
		public List<Note> NoteList;
		public List<Note> LandMineList;
		public Line()
		{
			NoteList = new List<Note>()
			{
				Capacity = 225
			};
			LandMineList = new List<Note>()
			{
				Capacity = 20
			};
		}
	}


	public abstract class BMSObject: System.IComparable<BMSObject>
	{
		public int Bar { get; protected set; }
		public double Beat { get; protected set; }
		public double Timing { get; set; } 

		public BMSObject(int bar, double beat, double beatLength)
		{
			Bar = bar;
			Beat = (beat / beatLength) * 4.0f;
		}

		public BMSObject(int bar, double beat)
		{
			Bar = bar;
			Beat = beat;
		}

		public int CompareTo(BMSObject other)
		{
			if (Bar != other.Bar)
			{
				return Bar.CompareTo(other.Bar);
			}
			else
			{
				return Beat.CompareTo(other.Beat);
			}
		}
	}
}