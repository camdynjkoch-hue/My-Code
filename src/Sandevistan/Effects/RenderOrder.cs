namespace Quicksilver.Sandevistan
{
    internal static class RenderOrder
    {
        public const int GradeTint = 3990;

        public const int GradeGlow = 3991;

        public const int Afterimage = 4550;

        /// <summary>
        /// The player's materials draw after the grade quad, so the grade never covers them.
        /// </summary>
        public static int PlayerQueue(int originalQueue)
        {
            if (originalQueue < 0)
                originalQueue = 2000;

            if (originalQueue > 5000)
                originalQueue = 5000;

            return 4000 + originalQueue / 5;
        }
    }
}
