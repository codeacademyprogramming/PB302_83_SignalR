namespace Pustok.Models
{
    public class TeacherSkill: BaseEntity
    {
        public int TeacherId { get; set; }
        public int SkillId { get; set; }
        public byte Percentage { get; set; }

        public Teacher Teacher { get; set; }
        public Skill Skill { get; set; }    
    }
}
