## Epidemic Simulator

*Video compression hates my simulator... too many little guys I guess*
https://github.com/user-attachments/assets/576b3315-616c-428a-a1af-d3d26ebcd6cf

### Introduction
This is a brief outline of my creation/thought process of an infectious disease simulator. The goal is to create a simulation based on the SIR model that encapsulates a mutating ‘flu-like’ virus. Ideally, I’ll be able to write a simple simulation that emulates the flu’s periodic infection rates.

### Tech Stuff
For this project, I’ll be working in the Unity game engine, specifically the data-oriented tech stack (DOTS). Why Unity? I’m familiar with it, and it deals with things like rendering, animations, and materials that I don’t want to worry about. I’m using DOTS for better efficiency, so we can run this simulation on the scale of a small population (100,000 residents).

### Basic SIR Model
The simulation takes place in a small town comprised of many buildings. Every day, residents find a random ‘building’ to inhabit. As per basic SIR models, residents have three states: susceptible, infected, and recovered. When a susceptible resident is in a room with an infected resident, the susceptible resident has a chance to become infected as well. For this simulation, I’ll say that the infection lasts a few days. After the infectious period, residents become recovered and are immune to infection.

Of course, this model doesn’t encapsulate the periodic nature of flu’s infection rates, since residents cannot lose immunity, and the virus itself does not mutate. Let’s just start with this basic SIR model before proceeding.

<p align="center">
<img width="1659" height="926" alt="Screenshot 2025-08-26 164320" src="https://github.com/user-attachments/assets/3940421a-04fa-4670-bc63-eca15f19bac5" />
</p>

### Temporary Immunity
To add a new bit of complexity to this simulation, I’ll try making immunity temporary. By making immunity disappear a couple in-simulation weeks after the initial infection, we can see an underdamped behavior in the ratio of susceptible, infected, and recovered residents.

<p align="center">
<img width="1660" height="928" alt="Screenshot 2025-08-26 164017" src="https://github.com/user-attachments/assets/b6943be3-f092-4468-8188-5fc30fc46a14" />
</p>

### Vaccines
We can further develop this simulation by adding vaccines. Vaccines are implemented as a new state that residents can assume: vaccinated. The vaccinated state gives residents permanent immunity to the virus. Every day, a small percentage of the uninfected population becomes vaccinated. Predictably, this results in the oscillating ratios going from underdamped to completely constant after the virus slowly dies out due to herd immunity.

<p align="center">
<img width="1654" height="926" alt="Screenshot 2025-08-26 164207" src="https://github.com/user-attachments/assets/7974d64e-b43a-44b7-9b85-4c7d2b76d444" />
</p>

### Mutations, Attempt One
I mentioned viral mutations earlier, so I’ll try adding a new mechanic that causes the virus to mutate after several in-simulation months. When mutating, all vaccinated and immune residents immediately become susceptible again. 

I have a couple of issues with this implementation. It feels kind of like ‘hard-coding’ the seasonal behavior, and it’s pretty unrealistic. This would only really occur if (somehow) the mutation occurred simultaneously in all infected people, which is obviously very unrealistic. Let’s continue searching for a more elegant solution.

<p align="center">
<img width="1653" height="926" alt="Screenshot 2025-08-26 163852" src="https://github.com/user-attachments/assets/718eb4dc-8f02-46f8-9a00-e8aea6b167cd" />
</p>

### Mutations, Attempt Two

As of now, we see that the virus’s newly mutated form simply replaces the old one, resulting in the periodic behavior we expected. I wonder what happens if the mutation is granular instead? To implement this, we’ll have to modify the simulation. 
I’m going to remove vaccinations and temporary immunity from the equation (just to make implementation easier for now… they might get added back in later).
The virus will now carry a ‘gene,’ which is simply a single integer value.
Every time a resident gets infected with the virus, the virus they carry will slightly nudge its gene number up or down. For example, a virus with the gene #120 might change to #121.
When residents become immune to a virus, they gain immunity to all viruses whose genes are within a threshold of that specific gene. For example, a resident immune to #120 would also be immune to #122, but still susceptible to #100.
For the sake of simplicity, we’ll assume that residents can only be infected by a single virus at a time and can only be immune ot one gene at a time.
To make mutations more observable, I’ll add some new colors. When the virus mutates, its color will turn slightly more purple. This way, we can see more clearly how mutated a virus is. Once a virus has mutated to become entirely purple, if it continues mutating, I’ll just have the virus loop back to the original red color.

After tuning the behavior, I noticed several issues with the simulation’s current state:
Residents being immune to a single gene at a time results in about two or three different viruses always swarming the population, while the total number of infected is about constant after the initial infection period.
It’s hard to tell how viruses are spreading when the residents constantly randomize their room positions.
Additionally, since residents pick entirely random rooms every tick, there’s not much of a point to seeing the population’s movement, since the graph can encapsulate all the major information.

### Mutations, Attempt Three
To remedy those prior issues, I’m going to implement several major changes:
Instead of choosing an entirely random room each tick, residents will instead choose a neighboring room to move to each tick. Besides giving the visualization a reason for existing, this also distinguishes the ‘simulated’ aspect of this simulation from the purely mathematical solution used in SIR models.
Virus mutation genes will only increase now. This way, I can see how far the virus has mutated by how high its gene number is.
Residents will now be immune to all viruses with mutation gene numbers lower than their current immunity gene. For example, a resident who is immune to the #100 gene would also be immune to viruses carrying the #80 gene. This effectively allows residents to retain immunity to older ‘versions’ of the virus.

The result is pretty interesting. The virus creeps slowly around the population, and when it’s close to dying out, it will sometimes mutate and cause a large resurgence of infected residents.

<p align="center">
<img width="1656" height="927" alt="Screenshot 2025-08-31 204732" src="https://github.com/user-attachments/assets/d672578a-4980-4569-bf6f-1a6a7b7723f8" />
</p>

I noticed that after the initial spread of newly mutated viruses, the virus would slowly fade away as everyone gained immunity. It made me wonder what would happen if I added the ‘temporary immunity period’ back in. For example, if residents became susceptible again after 60 in-game days.

<p align="center">
<img width="1655" height="926" alt="Screenshot 2025-08-31 205110" src="https://github.com/user-attachments/assets/0d4d6569-b89e-424a-b940-5341ccffd5e6" />
</p>

The residents get infected in waves similar to before, but there are also periods of brief stability before waves of previously recovered (now susceptible) residents reappear and get infected. The larger infected spikes in the graph indicate moments when the virus also mutated on top of many residents losing their immunity. This behavior is noticeably volatile.

### Deaths
While the volatile behavior is very interesting, I don’t think it’s entirely accurate. Allegedly, people don’t lose immunity to viruses they’ve been infected by after just 30 or 60 days. So, to replace the temporary immunity, I’ll have to find another way to add new susceptible people into the simulation. I’ve decided to experiment with births and deaths. The concept is quite simple:
Every time a resident reaches the end of their infected period, there is a small chance that instead of becoming ‘recovered’, they become ‘deceased’ instead.
Then, every in-game day, a portion of the ‘deceased’ residents is replaced with fresh, susceptible residents.

<p align="center">
<img width="1659" height="928" alt="Screenshot 2025-08-31 215213" src="https://github.com/user-attachments/assets/102ccaf0-517b-4b60-a870-216a966239f9" />
</p>

After implementing it, I realized that this approach suffers from a few shortcomings. The main issue is that, unless I crank the death chance up very high, the virus still doesn’t have enough susceptible people to sustain itself at a low rate when not mutating. This results in the above extremely volatile graph where the virus may quickly mutate multiple times like before, but quickly die out by chance when it doesn’t mutate in time. 

My first try at mitigating this volatile behavior was to increase the range that residents can move in a single day. I hypothesized that allowing the residents to move more would allow infected residents to reach susceptible residents more easily during the sparse phases of infection.

<p align="center>
<img width="1655" height="927" alt="Screenshot 2025-08-31 215517" src="https://github.com/user-attachments/assets/2cc4d67c-ac4a-43fe-8c0e-4949c1f71245" />
</p>

The result is… even more volatile. The virus spreads extremely quickly, mutates quickly, but still dies out nonetheless. Instead of continuing to experiment and toy with this simulation, I think the best method to truly emulate the recurring nature of the flu is to read some of the science explaining it first.

[Understanding The Recurrence Of Influenza: Does The Flu Come Back? | MedShun](https://medshun.com/article/does-the-flu-come-back)

[How Long Does COVID Immunity After Infection Last? - Biology Insights](https://biologyinsights.com/how-long-does-covid-immunity-after-infection-last/)

It turns out that immunity does, in fact, fade over time. It seems I should read the science a bit more before committing to these ideas. To better emulate the seasonal nature, I’ll try adding the temporary immunity back in. Let’s see what happens.

<p align="center">
<img width="1659" height="926" alt="Screenshot 2025-08-31 220805" src="https://github.com/user-attachments/assets/3e67fba8-76f1-40df-aa1b-05071b3ad964" />
</p>

It works! We see the virus starts in periodic waves and also has segments of rapid mutation. The result is a self-sustaining, periodic virus.

### Using Monte Carlo Method
I recently talked with an experienced programmer and shared this epidemic simulator. They suggested I try tinkering with the Monte Carlo method and see what I can discover. I hadn’t heard of the Monte Carlo method prior, so it piqued my interest. I tried implementing it on the simulation, having it run many iterations and track the overall ‘average’ of all the results.

<p align="center>
<img width="1656" height="929" alt="Screenshot 2025-09-02 153115" src="https://github.com/user-attachments/assets/38398b7b-cf70-4bac-9b27-52e174c6c74f" />
</p>

You can see on the bottom left of the screenshot is all the graphs of all the simulation runs overlayed, and to the right is the average of all those graphs combined. Interestingly, the average graph seems to exhibit some slightly underdamped behavior, similar to the ordinary differential equations this simulation was based on.

I also interpreted the left graph. It seems to show a very consistent ODE behavior during the virus’s initial spreading period, but after that, the period and amplitude of the virus’s spread becomes highly variable, depending on how the spatial and mutation factors unfold randomly.

Just as a sanity check, I’ve also simulated with the mutation rate set to zero. I expect that the left graph should be much stabler, and the ‘average’ graph will show clear underdamped behavior.

<p align="center">
<img width="1657" height="925" alt="Screenshot 2025-09-02 153434" src="https://github.com/user-attachments/assets/385611d5-dac8-4e1d-ad2c-2e4b8481d9f6" />
</p>

While my hypothesis was mostly correct, there’s a few things I’m beginning to notice by applying the Monte Carlo method. Firstly, I noticed that because of the simulation space’s rectangular shape and the resident’s movement behavior, residents tend to clump around the corner and edges of the simulation. Secondly, it seems that the left graph has a few cases where the virus dies out nearly immediately. This affects the average and results in the ‘susceptible’ line in the average graph being slightly higher than its counterpart on the left. After some testing, I believe that this is caused by the simulation’s inherent randomness. Since the chance of infection in those runs wasn’t extremely high, there was a small chance that the virus died with the initial case.

For the first point, I’ll allow residents to ‘wrap’ around the space, sort of like Pac-Man. Basically, a resident at the edge of the space can move past the edge and wrap around to the other side of the space. This way I can remove the bias that comes with the shape of the simulation space.

After toying around with the Monte Carlo simulation a bit, I found some fun patterns. For example, we can see that a virus with 50% mortality rate levels off quickly, since the constant introduction of new susceptibles from the deaths allows the virus to sustain itself at a stable, non-oscillating rate.

<p align="center">
<img width="1657" height="923" alt="Screenshot 2025-09-02 160106" src="https://github.com/user-attachments/assets/6a0f85e0-8105-4c85-aa8f-2d9477b53b80" />
</p>

Meanwhile, in a low-mortality virus that stays in its host for a long time with a small chance of spreading, we see much longer, underdamped oscillations, as the population is hit with waves instead.

<p align="center">
<img width="1659" height="927" alt="Screenshot 2025-09-02 160411" src="https://github.com/user-attachments/assets/638aa76e-091e-4e62-a926-cf47070e7c65" />
</p>

Reintroducing mutations, let’s experiment more. I expected a virus with a relatively high chance of mutating to cause extremely volatile behavior. However, because the virus would continually mutate across the population, it kept the infection rate extremely stable, since a majority of the population was always infected at some time.

<p align="center">
<img width="1656" height="924" alt="Screenshot 2025-09-02 160709" src="https://github.com/user-attachments/assets/84a6096b-88ca-4740-9b6d-dd125b403671" />
</p>

In contrast, a virus with a smaller chance of mutating causes much more unpredictable behavior. 

<p align="center">
<img width="1656" height="922" alt="Screenshot 2025-09-02 160840" src="https://github.com/user-attachments/assets/29f0c334-c0a3-4e8d-80a0-3634a51301e6" />
</p>

How about that same virus, but with a much higher mortality rate?

<p align="center">
<img width="1658" height="927" alt="Screenshot 2025-09-02 161140" src="https://github.com/user-attachments/assets/a62222a7-00d0-4592-bcf2-fca6c3c5aa1f" />
</p>

That’s enough tinkering for now. What I’ve noticed throughout all of these little tests is that the Monte Carlo simulation allows me to tweak the simulation parameters very slightly between runs and more clearly understand what each parameter does. It offers a kind of intuition into the simulation’s parameters, and I think that’s pretty neat :)

### Conclusion
I think I’ll wrap up the SIR model here. I’ve thoroughly enjoyed this light exploration of SIR models, and I think I may return to it in the future when I have more ideas. Through this project, I learned about the basics of modeling epidemics, an intuition for creating models of real-world phenomenon, parallel computing, and Monte Carlo simulations.
