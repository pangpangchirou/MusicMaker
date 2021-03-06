using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Manufaktura.Model;
using Manufaktura.Controls;
using Manufaktura.Controls.WPF;
using Manufaktura.Music;
using Manufaktura.Controls.Model;
using Manufaktura.Model.MVVM;
using Manufaktura.Music.Model;
using Manufaktura.Music.Model.MajorAndMinor;
using System.IO;
using Manufaktura.Controls.Parser;
using System.Xml;
using System.Xml.Linq;
using System.Windows;
using System.Runtime.Serialization.Formatters.Binary;
using Manufaktura.Controls.Audio;
using Manufaktura.Controls.Desktop.Audio;

namespace Piano
{
    public class ScoreVM : ViewModel
    {
        private string fileName = "";   // Name of the file to load from or save to.
        private ScorePlayer player;

        public PlayCommand PlayCommand { get; }
        public StopCommand StopCommand { get; }

        public ScoreVM()
        {
            PlayCommand = new PlayCommand(this);
            StopCommand = new StopCommand(this);
        }
        
        public ScorePlayer Player => player;
        private Score data;
        /// <summary>
        /// Holds the current Score object. Refreshing the public property updates the viewer.
        /// </summary>
        public Score Data
        {
            get { return data; }
            set { data = value; OnPropertyChanged(() => Data); if (player != null) ((IDisposable)player).Dispose(); //This is needed in Midi player. Otherwise it can throw a "Device not ready" exception.
                player = new MidiTaskScorePlayer(data);
                OnPropertyChanged();
                OnPropertyChanged(() => Player);
                PlayCommand?.FireCanExecuteChanged();
                StopCommand?.FireCanExecuteChanged();
            }
        }

        private Key keySig;
        /// <summary>
        /// Holds the current key signature
        /// </summary>
        public Key KeySig
        {
            get { return keySig; }
            set { keySig = value; }
        }

        private TimeSignature timeSig;
        /// <summary>
        /// Holds the current time signature
        /// </summary>
        public TimeSignature TimeSig
        {
            get { return timeSig; }
            set { timeSig = value; }
        }

        /// <summary>
        /// Populates the note viewer with an empty single staff. Does not set fileName.
        /// </summary>
        public void loadStartData()
        {
            Data = createStartingStaff();   // This will switch to createGrandStaff once the note entry bugs are fixed
        }



        /// <summary>
        /// Generates an empty grand staff in the specified key and time signature.
        /// </summary>
        /// <param name="key">A Key enumeration value to specify the starting key of the piece</param>
        /// <param name="timeSig">A TimeSignature object representing the time signature of the piece</param>
        /// <returns>A score object containing a grand staff in the specified key and time signature</returns>
        public Score createStartingStaff()
        {
            // Create a score object with a single staff
            var score = Score.CreateOneStaffScore();
            // Add treble clef
            score.FirstStaff.Elements.Add(Clef.Treble);
            // add key signature. The 0 in the Key constructor means no sharps or flats. 
            // negative numbers are flat keys, positive for sharp keys.
            keySig = new Key(0);
            score.FirstStaff.Elements.Add(keySig);
            // Set time sig to 4/4
            timeSig = TimeSignature.CommonTime;
            score.FirstStaff.Elements.Add(timeSig);
            // Add some notes for test purposes
            score.FirstStaff.Elements.Add(new Note(Pitch.C5, RhythmicDuration.Quarter));
            score.FirstStaff.Elements.Add(new Note(Pitch.B4, RhythmicDuration.Quarter));
            score.FirstStaff.Elements.Add(new Note(Pitch.C5, RhythmicDuration.Half));
            score.FirstStaff.Elements.Add(new Barline());
            return score;
        }



        /// <summary>
        /// Generates an empty grand staff in the specified key and time signature.
        /// </summary>
        /// <param name="key">A Key enumeration value to specify the starting key of the piece</param>
        /// <param name="timeSig">A TimeSignature object representing the time signature of the piece</param>
        /// <returns>A score object containing a grand staff in the specified key and time signature</returns>
        public Score createGrandStaff(Key key, TimeSignature timeSig)
        {
            var score = Score.CreateOneStaffScore();    // See createStartingStaff for line by line comments
            score.FirstStaff.Elements.Add(Clef.Treble);
            this.keySig = key;
            score.FirstStaff.Elements.Add(keySig);
            this.timeSig = timeSig;
            score.FirstStaff.Elements.Add(timeSig);
            score.FirstStaff.Elements.Add(new Note(Pitch.C5, RhythmicDuration.Quarter));
            score.FirstStaff.Elements.Add(new Note(Pitch.B4, RhythmicDuration.Quarter));
            score.FirstStaff.Elements.Add(new Note(Pitch.C5, RhythmicDuration.Half));
            score.FirstStaff.Elements.Add(new Barline());
            Staff bass = new Staff();
            bass.Elements.Add(Clef.Bass);
            bass.Elements.Add(key);
            bass.Elements.Add(timeSig);
            // Add bass staff
            score.Staves.Add(bass);
            keySig = key;
            return score;
        }


        /// keep this for later
        /// <summary>
        /// Returns a default empty grand staff.
        /// </summary>
        /// <returns>A Score object containing an empty grand staff in C major and 4/4 time</returns>
        public Score createGrandStaff()
        {
            return createGrandStaff(new Key(0), TimeSignature.CommonTime);
        }



        /// <summary>
        /// Loads the viewer with a Score object generated from the specified MusicXml file.
        /// </summary>
        public void loadFile(string fileName)
        {
            this.fileName = fileName;
            if (File.Exists(fileName))
            {
                // Parser instance converts between Score object and MusicXML
                var parser = new MusicXmlParser();
                Score score = parser.Parse(XDocument.Load(fileName)); // Load the content of the specified file into Data
                Data = score;
            }
            else throw new FileNotFoundException(fileName + " not found."); //This and any parser exceptions will be caught by the calling function
        }



        /// <summary>
        /// Creates a new score with the specified staff configuration and loads it into the viewer.
        /// </summary>
        /// <param name="title">The title of the piece</param>
        /// <param name="staves">An array of Staff objects representing the different parts in the composition.</param>
        public void createNew(string title, Staff[] staves)
        {
            Score score = new Score();
            foreach (Staff staff in staves)
            {
                score.Staves.Add(staff);
            }
            // TODO: Figure out how to add a title. This may have to be done directly to the MusicXML while saving.
            // Replace existing socre object with newly defined score
            Data = score;
        }



        /// <summary>
        /// Saves the current Score as a MusicXML file.
        /// PENDING PARSER IMPLEMENTATION FROM MANUFACTURA
        /// </summary>
        /// <returns></returns>
        public bool save()
        {
            var setting = data.FirstStaff.MeasureAddingRule;

            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            if (fileName == "" || !File.Exists(fileName))
            {
                dialog.DefaultExt = "mml";
                dialog.CheckPathExists = true;
                dialog.ValidateNames = true;

                dialog.Filter = "Music Maker Score|*.mms|Music XML|*.mml";


                Nullable<bool> result = dialog.ShowDialog();
                if (result == false) return false;
                fileName = dialog.FileName;
            }

            if (dialog.FilterIndex == 1)    // Native binary format
            {
                //Score
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    byte[] outBytes;
                    using (var ms = new MemoryStream())
                    {
                        foreach (Staff s in data.Staves)
                        {
                            foreach (MusicalSymbol item in s.Elements)
                            {
                                if (item.GetType().IsSubclassOf(typeof(NoteOrRest)) || item.GetType() == typeof(Barline))
                                    bf.Serialize(ms, item);
                            }
                        }
                        bf.Serialize(ms, data);
                        outBytes = ms.ToArray();
                    }
                    using (FileStream stream = new FileStream(fileName, FileMode.Create))
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                        {
                            writer.Write(outBytes);
                            writer.Close();
                        }
                    }
                }

                catch (Exception ex)
                {
                    MessageBox.Show("Could not save to " + fileName + ".\n" + ex.Message);
                    fileName = "";
                    save();
                }
            }
            else try    // MusicXML format (not yet implemented in Manufactura)
            {
                var parser = new MusicXmlParser();
                var outputXml = parser.ParseBack(data);
                outputXml.Save(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save to " + fileName + ".\n" + ex.Message);
                fileName = "";
                save();
            }

            return true;
        }


        /// <summary>
        /// Forces the Score object to refresh its property tree. Have to do this
        /// to update the content of the bound NoteViewer.
        /// </summary>
        public void updateView()
        {
            var score = Data;
            Data = null;
            Data = score;
        }

        ////// IN PROGRESS ////////
        /// <summary>
        /// Adds a barline to the current measure if needed, and breaks the last note of the measure if it
        /// exceeds the allowed beats/measure, inserting the remainder into the next measure, then recursively calls
        /// itself on subsequent measures until it hits a measure that does not overflow.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="ts"></param>
        internal void fitMeasure(Measure m, TimeSignature ts) // Still need to add support for deletion
        {
            bool nextMeasureChanged = false;
            foreach (MusicalSymbol item in m.Elements)
            {
                // If new time signature, update
                if (item.GetType() == typeof(TimeSignature)) ts = (TimeSignature)item;

                // Make sure that any cleffs and signatures are at the beginning of the measure, before notes and rests
                if (item.GetType() == typeof(TimeSignature) || item.GetType() == typeof(Key) || item.GetType() == typeof(Clef))
                {
                    NoteOrRest firstBeat = getFirstBeat(m);
                    if (m.Elements.IndexOf(firstBeat) < m.Elements.IndexOf(item)) swapPositions(m.Staff, item, firstBeat);
                }
            }

            // While the current measure is not full and is not the end of the song, steal the first beat from the next measure
            while (getDurations(m).Sum() < ts.WholeNoteCapacity 
                && m.Number < m.Staff.Measures.Count 
                && getFirstBeat(getNextMeasure(m)) != null)
            {
                Measure next = getNextMeasure(m);
                NoteOrRest nr = getFirstBeat(next);
                next.Elements.Remove(nr);
                m.Elements.Add(nr);     // This note might be longer than we have room for, but the next loop will take care of it
                nextMeasureChanged = true;
            }

            Staff staff = m.Staff;
            int measureIndex = staff.Measures.IndexOf(m);

            // While the current measure has too many beats, push the extra into the next measure
            while (getDurations(m).Sum() > ts.WholeNoteCapacity)
            {
                //double[] d = getDurations(m);
                double overage = getOverage(m, ts);// d.Sum() - ts.WholeNoteCapacity;
                NoteOrRest lastNoteOrRest = (NoteOrRest)m.Elements.Last(e => e.GetType().IsSubclassOf(typeof(NoteOrRest)));
                double lastDurationValue = lastNoteOrRest.Duration.ToDouble();
                int lastItemIndex = staff.Elements.IndexOf(lastNoteOrRest);
                NoteOrRest itemToMove = null;

                // If the overage is because the last note is too big, break it into two notes
                if (lastDurationValue > overage)
                {
                    // Shorten the duration of the culprit to fit the measure
                    //lastNoteOrRest.Duration = toRhythmicDuration(lastDurationValue - overage);

                    // Instead of shortening in place, try replacing so the viewer notices the change
                    NoteOrRest itemInPlace;

                    // Propagate the change to the staff -- Never mind. It propagates fine here.
                    //m.Staff.Elements[m.Staff.Elements.IndexOf(lastNoteOrRest)] = null;
                    //m.Staff.Elements[m.Staff.Elements.IndexOf(lastNoteOrRest)] = lastNoteOrRest;


                    // Add a copy of the culprit note or rest at the remaining duration
                    if (lastNoteOrRest.GetType() == typeof(Note))
                    {
                        itemInPlace = new Note(((Note)lastNoteOrRest).Pitch, toRhythmicDuration(lastDurationValue - overage));
                        itemToMove = new Note(((Note)lastNoteOrRest).Pitch, toRhythmicDuration(overage));
                    }
                    else
                    {
                        itemInPlace = new Rest(toRhythmicDuration(lastDurationValue - overage));
                        itemToMove = new Rest(toRhythmicDuration(overage));
                    }
                    m.Staff.Elements.Remove(lastNoteOrRest);
                    m.Elements.Remove(lastNoteOrRest);
                    m.Staff.Elements.Insert(lastItemIndex, itemInPlace);
                }
                else
                {
                    itemToMove = lastNoteOrRest;
                    m.Staff.Elements.Remove(lastNoteOrRest);
                    m.Elements.Remove(lastNoteOrRest);
                    lastItemIndex--;
                }

                // Grab the next barline and put it after the last note of the adjusted measure
                //Barline bar = moveOrAddBarlineAfter(staff.Elements[lastItemIndex -1 ]);   // This is adding it to the staff elements, which have not been refreshed

                // Refresh the updated measure in the staff.measures collection

                if (m.Elements[m.Elements.Count - 1].GetType() != typeof(Barline)) m.Staff.Elements.Insert(lastItemIndex + 1, new Barline()); // may have to use "add"

                //*** May need to remove m from staff.measures and insert updated version***

                // Add the new note/rest or new partial note/rest to the new measure
                // getNextMeasure(m).Elements.Insert(0, itemToMove);
                m.Staff.Elements.Insert(lastItemIndex + 2, itemToMove);

                // m.Staff.Elements.Insert(lastItemIndex + 1, lastElem);
                //staff.Elements.Insert(lastItemIndex + 2, itemToMove);
                updateView();
                //getNextMeasure(m).Elements.Insert(0, itemToMove);
                nextMeasureChanged = true;

                // Refresh measures against the current staff
                //m = null;
                //m = staff.Measures.ElementAt(measureIndex);
                //nextMeasure = null;
                //nextMeasure = staff.Measures.ElementAt(measureIndex + 1);

            }

            // If there are changes to the next measure, fix it.
            if (nextMeasureChanged) //fitMeasure(staff.Measures[staff.Measures.IndexOf(m) + 1], ts);
                fitMeasure(getNextMeasure(m), ts);
        }

        // Moves the next barline to the position immediately after the specified element. If there are no more barlines on
        // the staff, creates a new one.
        private Barline moveOrAddBarlineAfter(MusicalSymbol item)
        {
            var elems = item.Staff.Elements;
            int itemPosition = elems.IndexOf(item);
            Barline bar = (Barline) elems.FirstOrDefault(b => elems.IndexOf(b) > itemPosition && b.GetType() == typeof(Barline));
            if (bar == null) bar = new Barline();
            else item.Staff.Elements.Remove(bar);
            item.Staff.Elements.Insert(itemPosition + 1, bar);
            return bar;
        }

        // Helper methods to get the next measure and the first note or rest in a measure
        private Measure getNextMeasure(Measure m) { return m.Staff.Measures.Find(m2 => m2.Number == m.Number + 1); }
        private NoteOrRest getFirstBeat(Measure m) { return (NoteOrRest) m.Elements.Find(e => e.GetType().IsSubclassOf(typeof(NoteOrRest))); }

        // Get an array of doubles that represent the durations of all elements in a measure
        private double[] getDurations(Measure m)
        {
            double[] d = new double[m.Elements.Count];
            for (int i = 0; i < d.Length; i++)
            {
                var elem = m.Elements[i];
                if (elem.GetType().IsSubclassOf(typeof(NoteOrRest)))
                    d[i] = ((NoteOrRest)elem).Duration.ToDouble();
                else d[i] = 0;
            }
            return d;
        }

        // Returns te reference to the specified item on the staff
        private MusicalSymbol getItemOnStaff(MusicalSymbol ms)
        {
            return ms.Staff.Elements[0];
        }

        // Returns the total beat durations of notes and rests in the measure minus the allowed amount
        private double getOverage(Measure m, TimeSignature ts) {  return getDurations(m).Sum() - ts.WholeNoteCapacity; }

        // Returns the last note or rest in the measure
        private NoteOrRest getLast(Measure m)
        {
            for (int i = m.Elements.Count - 1; i >= 0; i--)
                if (m.Elements[i].GetType().IsSubclassOf(typeof(NoteOrRest))) return (NoteOrRest) m.Elements[i];
            return null;
        }

        // Helper method to switch positions of elements on a staff
        private void swapPositions(Staff staff, MusicalSymbol item1, MusicalSymbol item2)
        {
            int pos1 = staff.Elements.IndexOf(item1);
            int pos2 = staff.Elements.IndexOf(item2);
            staff.Elements.Remove(item1);
            staff.Elements.Insert(pos2, item1);
            staff.Elements.Remove(item2);
            staff.Elements.Insert(pos1, item2);
        }

        // Helper method to convert a double to a RhythmicDuration object.
        private RhythmicDuration toRhythmicDuration(double d)
        {
            RhythmicDuration rd;
            // Check fractions down to 1/32 and convert to the base note type
            if (d >= 1) rd = RhythmicDuration.Whole;
            else if (d >= .5) rd = RhythmicDuration.Half;
            else if (d >= .25) rd = RhythmicDuration.Quarter;
            else if (d >= .125) rd = RhythmicDuration.Eighth;
            else if (d >= .0625) rd = RhythmicDuration.Sixteenth;
            else rd = RhythmicDuration.D32nd;

            // Fill in dots if needed
            while (rd.ToDouble() < d)
            {
                rd.Dots++;
            }

            return rd;
        }

    }
    public abstract class PlayerCommand : System.Windows.Input.ICommand
    {

        public event EventHandler CanExecuteChanged;


        protected ScoreVM viewModel;

        protected PlayerCommand(ScoreVM viewModel)
        {
            this.viewModel = viewModel;
        }

        public void FireCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        public abstract bool CanExecute(object parameter);

        public abstract void Execute(object parameter);

    }

    public class PlayCommand : PlayerCommand
    {

        public PlayCommand(ScoreVM viewModel) : base(viewModel)
        {
        }



        public override bool CanExecute(object parameter)
        {
            return viewModel.Player != null;
        }

        public override void Execute(object parameter)
        {
            viewModel.Player?.Play();
        }

    }

    public class StopCommand : PlayerCommand
    {
        public StopCommand(ScoreVM viewModel) : base(viewModel)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return viewModel.Player != null;
        }

        public override void Execute(object parameter)
        {
            viewModel.Player?.Stop();
        }
    }
}
