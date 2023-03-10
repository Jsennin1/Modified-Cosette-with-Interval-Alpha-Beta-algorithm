using System.Collections.Generic;

namespace Cosette.Tuner.Common.Requests
{
    public class GenerationDataRequest
    {
        public int TestId { get; set; }
        public double ElapsedTime { get; set; }
        public double BestFitness { get; set; }

        public List<GeneDataRequest> BestChromosomeGenes { get; set; }
    }
}
