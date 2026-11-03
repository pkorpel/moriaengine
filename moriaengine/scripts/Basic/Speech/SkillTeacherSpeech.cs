/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#828 - Erebor's player rank (tag.skillrank: Novic, Expert, Mistr, Velmistr), earned through the rank
	/// quests (#833). Gates the skill teachers' lessons when SkillTeacherSettings.requireRank is on.</summary>
	public enum SkillRank {
		Novice = 0,
		Expert = 1,
		Master = 2,
		Grandmaster = 3
	}

	public partial class Player {
		private static readonly TagKey skillRankTK = TagKey.Acquire("_skillRank_");

		/// <summary>#828 - Erebor's rank (tag.skillrank), kept on the profession plugin (#951). The tag only holds it for
		/// someone with no profession, and it's where every rank was before: whichever comes here first moves it over.</summary>
		public SkillRank SkillRank {
			get {
				ProfessionPlugin plugin = this.ProfessionPlugin;
				object tag = this.GetTag(skillRankTK);
				if (tag == null) {
					return (plugin == null) ? SkillRank.Novice : plugin.SkillRank;
				}
				SkillRank rank = RankFromTag(tag);
				if (plugin != null) {
					plugin.SkillRank = rank;
					this.RemoveTag(skillRankTK);
				}
				return rank;
			}
			set {
				ProfessionPlugin plugin = this.ProfessionPlugin;
				if (plugin != null) {
					plugin.SkillRank = value;
					this.RemoveTag(skillRankTK);
				} else if (value == SkillRank.Novice) {
					this.RemoveTag(skillRankTK);
				} else {
					this.SetTag(skillRankTK, value);
				}
				this.RefreshTitle();
			}
		}

		//the #265 boxed-int coercion
		private static SkillRank RankFromTag(object tag) {
			if (tag is SkillRank rank) {
				return rank;
			}
			if ((tag is int intTag) && Enum.IsDefined(typeof(SkillRank), intTag)) {
				return (SkillRank) intTag;
			}
			return SkillRank.Novice;
		}
	}

	/// <summary>#828 - Erebor's [SPEECH spk_mistr] (systems/mistri.scp), on its 36 skill teachers (NPCDef.TeachesSkill,
	/// one skill each). Skills grow normally past 25/50/75 (Erebor's skillgain only checks the profession cap); a
	/// teacher sells the jump from exactly 25.0/50.0/75.0 to 30.0/55.0/80.0 - Expert, Master and Grandmaster lessons.
	/// Greeting it ("dobry den", "mistr", "hi", "hello", "buy"...) gets the offer, "ano"/"yes" buys it. A lesson needs
	/// the skill exactly on its threshold, a profession that allows the new value, a high enough level and - with
	/// SkillTeacherSettings.requireRank - the player's rank; Erebor's own messages tell which one is missing.
	/// Not ported: the weekly "meet a master" quest (f_weekly_meetMaster) - no weekly quests in the engine.
	/// Replaces the converted [SpeechStub spk_mistr].</summary>
	public sealed class SkillTeacherSpeech : AbstractSpeech {
		private static readonly string[] greetings = { "dobry", "den", "mistr", "expert", "zdravim", "hi", "hello", "buy" };
		private static readonly string[] yeses = { "yes", "ano" };

		private sealed class Lesson {
			internal readonly SkillRank rank;
			internal readonly int threshold, target;
			internal readonly string title; //Erebor's instrumental: "stati se EXPERTEM"

			internal Lesson(SkillRank rank, int threshold, int target, string title) {
				this.rank = rank;
				this.threshold = threshold;
				this.target = target;
				this.title = title;
			}

			internal int Price {
				get {
					SkillTeacherSettings settings = SkillTeacherSettings.instance;
					return (this.rank == SkillRank.Expert) ? settings.expertPrice : (this.rank == SkillRank.Master) ? settings.masterPrice : settings.grandmasterPrice;
				}
			}

			internal int MinLevel {
				get {
					SkillTeacherSettings settings = SkillTeacherSettings.instance;
					return (this.rank == SkillRank.Expert) ? settings.expertMinLevel : (this.rank == SkillRank.Master) ? settings.masterMinLevel : settings.grandmasterMinLevel;
				}
			}
		}

		//checked highest first, as Erebor's canbe_velmistr / canbe_mistr / canbe_expert
		private static readonly Lesson[] lessons = {
			new Lesson(SkillRank.Grandmaster, 750, 800, "VELMISTREM"),
			new Lesson(SkillRank.Master, 500, 550, "MISTREM"),
			new Lesson(SkillRank.Expert, 250, 300, "EXPERTEM"),
		};

		public new static void Bootstrap() {
			new SkillTeacherSpeech("spk_mistr").Register();
		}

		private SkillTeacherSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			NPC teacher = listener as NPC;
			Player student = speechArgs.Speaker as Player;
			string speech = speechArgs.Speech;
			if ((teacher == null) || (student == null) || (speech == null)) {
				return SpeechResult.IgnoredOrActedUpon;
			}
			NPCDef def = teacher.Def as NPCDef;
			SkillName? skill = (def == null) ? null : def.TeachesSkill;
			if (skill == null) {
				return SpeechResult.IgnoredOrActedUpon;
			}
			//one Sphere block: the first matching ON= wins, the greetings come before ANO/YES
			if (ContainsAny(speech, greetings)) {
				Offer(teacher, student, skill.Value);
			} else if (ContainsAny(speech, yeses)) {
				Teach(teacher, student, skill.Value);
			}
			return SpeechResult.IgnoredOrActedUpon;
		}

		internal static bool ContainsAny(string speech, string[] words) {
			foreach (string word in words) {
				if (speech.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) {
					return true;
				}
			}
			return false;
		}

		private static void Offer(NPC teacher, Player student, SkillName skill) {
			if (!CanTalk(teacher, student)) {
				return;
			}
			WeeklyQuests.OnGreetTeacher(teacher, student, skill); //#1090 - f_weekly_meetMaster
			if (SkillTeacherSettings.instance.roundSkillOnGreeting) {
				RoundSkill(student, skill);
			}
			if (!IsFitForLessons(student)) {
				return;
			}
			Lesson lesson = FindLesson(student, skill);
			if (lesson != null) {
				teacher.Say(string.Format(CultureInfo.InvariantCulture,
					"Vidim ze jsi pripraven stati se {0} {1}. Pokud mas zajem staci rici ANO a za poplatek {2}gp te vytrenuji v tomto oboru.",
					lesson.title, SkillDisplayName(skill), lesson.Price));
			} else {
				teacher.Say("Nemohu te naucit vice o " + SkillDisplayName(skill) + ".");
			}
		}

		private static void Teach(NPC teacher, Player student, SkillName skill) {
			if (!CanTalk(teacher, student) || !IsFitForLessons(student)) {
				return;
			}
			Lesson lesson = FindLesson(student, skill);
			if (lesson == null) {
				teacher.Say("Nemohu te naucit vice o " + SkillDisplayName(skill) + ".");
				return;
			}
			if (GoldUtils.Pay(student, lesson.Price, notEnough: "")) {
				student.SetRealSkillValue((int) skill, lesson.target);
				teacher.Say("Byl jsi vytrenovan " + lesson.title + " " + SkillDisplayName(skill));
			} else {
				teacher.Say("Nemas patricnou sumu, za kterou bych te mohl vytrenovat.");
			}
		}

		private static bool CanTalk(NPC teacher, Player student) {
			if (student.Flag_Dead) {
				teacher.Say("Musis byt nazivu. S duchy se nebavim...");
				return false;
			}
			return true;
		}

		//Erebor: a player with no profession is a broken character; one under a stat spell can't take a lesson (ismagic)
		private static bool IsFitForLessons(Player student) {
			SkillTeacherLoc loc = Loc<SkillTeacherLoc>.Get(student.Language);
			if (student.Profession == null) {
				student.SysMessage(loc.DamagedCharacter);
				return false;
			}
			if (IsUnderMagic(student)) {
				student.SysMessage(loc.UnderMagic);
				return false;
			}
			return true;
		}

		/// <summary>Erebor's ismagic: bless, curse and the stat spells (strength, agility, cunning and their curses).</summary>
		public static bool IsUnderMagic(Character ch) {
			foreach (Plugin plugin in ch.GetAllPlugins()) {
				if ((plugin is StrModSpellEffectPlugin) || (plugin is DexModSpellEffectPlugin) || (plugin is IntModSpellEffectPlugin) ||
						(plugin is BlessSpellEffectPlugin) || (plugin is CurseSpellEffectPlugin)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>The lesson the student can take now, or null - telling them why not, as Erebor's canbe_* do:
		/// each lesson only speaks up when the skill is in its range (up to 25.0, 25.1-50.0, 50.1-75.0).</summary>
		private static Lesson FindLesson(Player student, SkillName skill) {
			SkillTeacherLoc loc = Loc<SkillTeacherLoc>.Get(student.Language);
			int value = student.GetRealSkillValue((int) skill);
			int cap = student.Profession.GetSkillCap(skill);
			foreach (Lesson lesson in lessons) {
				int rangeStart = lesson.threshold - 250;
				if ((value > lesson.threshold) || ((lesson.rank != SkillRank.Expert) && (value <= rangeStart))) {
					continue;
				}
				if (SkillTeacherSettings.instance.requireRank && (student.SkillRank < lesson.rank)) {
					student.SysMessage(loc.NotReadyYet);
				} else if (cap < lesson.target) {
					student.SysMessage(string.Format(CultureInfo.InvariantCulture, loc.ReachedYourMaximum, SkillDisplayName(skill)));
				} else if (value != lesson.threshold) {
					student.SysMessage(loc.TrainMore);
				} else if (student.Level < lesson.MinLevel) {
					student.SysMessage(loc.LevelTooLow);
				} else {
					return lesson;
				}
				return null;
			}
			return null;
		}

		/// <summary>Erebor's roundskill: the skill to the nearest 5.0, never below the profession's starting value.</summary>
		public static void RoundSkill(Player student, SkillName skill) {
			int value = student.GetRealSkillValue((int) skill);
			int rounded = ((value + 25) / 50) * 50;
			ProfessionDef profession = student.Profession;
			if (profession != null) {
				rounded = Math.Max(rounded, profession.GetSkillMinimum(skill));
			}
			if (rounded != value) {
				student.SetRealSkillValue((int) skill, rounded);
			}
		}

		private static string SkillDisplayName(SkillName skill) {
			AbstractSkillDef def = AbstractSkillDef.GetById((int) skill);
			return (def == null) ? skill.ToString() : def.Key;
		}
	}

	public class SkillTeacherLoc : CompiledLocStringCollection<SkillTeacherLoc> {
		public string DamagedCharacter = "Your character is damaged... use your crystal or call a GM.";
		public string UnderMagic = "You must not be under any magical influence.";
		public string NotReadyYet = "You are not ready for the next lesson yet.";
		public string ReachedYourMaximum = "You have reached your maximum in {0}.";
		public string TrainMore = "You must train more before the next lesson.";
		public string LevelTooLow = "Your level is too low.";
	}
}
