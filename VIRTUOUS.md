# Virtuous Coding Assignment

## Project Choice and Issue
I looked at several repos and had a hard time finding an open issue that I thought fit th scope of this exercise. I had not heard of this CMS before I was interested in looking into it in general. While looking over the issues, I noticed this one that was related to toasts and specifically accessibility. When I worked as a contractor for the VA, I spent a lot of time working on Section 508 compliance. Section 508 dictates the required level of accessibility for all government websites. Specifically, Section 508 requires compliance with WCAG 2.0 (Web Content Accessibility Guidelines). For my work with the VA, that even extended to the use of screen readers and keyboard-only navigation. Accessibility is something I'm passionate about, and WCAG itself can be a little tricky. 

Dynamic content on websites like Toast is a complex issue from an accessibility standpoint. With a screen reader, when malformed, it can be obtrusive and still focus, or worse, not give users any helpful feedback. In short, I picked this issue because it ties into my interest, and I think it's something that I can positively contribute to. 

## What Needs to be Done
We need to approach this from two different fronts:
1. The first front is the toast itself. If toasts aren't going to automatically dismiss, we need a way to close them. That means adding some sort of close functionality to the existing toasts, including the success toast. At this point, we don't know if non-success toasts close automatically, so we'll need to investigate that as well. We also don't know if the existing toasts, both success and non-success, are WCAG compliant, so we need to investigate and potentially fix them.
2. The second front that we need to handle is the configuration portion. This needs to go somewhere in the admin panel, but we're not exactly sure where yet. I did look briefly, and I couldn't find a clear spot where it would fit, so we'll need to investigate what options are available. Timeouts might already be configurable, but there is just no configuration point in the admin panel to do so.  

I spent a few minutes looking for a skill or MCP that would help with WCAG/ARIA but couldn't find one. I will need to point the agent to official documentation to help guide it. Ideally would want to test with a screen reader but those are difficult to use. I added [WAVE Evaluation Tool](https://chromewebstore.google.com/detail/wave-evaluation-tool/jbbplnpkjmmeebjpijfedlgcdilocofh) to my browser as it might help. I also grabbed the following resources:
- https://www.w3.org/WAI/WCAG22/Understanding/status-messages.html
- https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-live 

## What to Investigate First
The first thing we need to investigate is how toast works in general right now. Some things we need to figure out are:
- Are there non-success toasts?
- Are those dismissable?
If non-success toasts exist and are compliant (i.e. not automatically dismissed and proper ARIA markup), it's simply extending that functionality to success toasts. If they are not compliant, we need to fix those as well. 

## Worklog
1. I created `/specs/toast-access` which I will use as my agent handoff location. Normally don't commit but will be committed for this exercise.
2. I started with the following prompt (Opus 5 High):
    > Our goal here is to improve the accessibility of toasts in this application. Currently success toasts auto-dismiss after 5 seconds. This potentially violates WCAG 2.2 standards. To start we need to catalog how toasts currently work in this application. Create a document in /specs/toast-access that answers the following: What type of toasts are there? How does auto-dismiss work? Are there any toasts that don't auto-dismiss? If so, how are they closed? How is the auto-dismiss timeout configured? Do the current toasts have appropriate ARIA markup? Use @specs/toast-access/resources.md for information about ARIA in this context. You are an orchestrator. Use sub-agents as needed and consider using cheaper model for them. 
3. I reviewed the findings in @specs/toast-access/toast-catalog.md. Some of the findings were good (already a manual close) but also bad (3 separate implementations) and improper `aria-live="assertive"`. 
4. I ran `/clear` and then prompted (Opus 5 High)
    > Using @specs/toast-access/toast-catalog.md as grounding need to figure out an appropriate place to add a configuration point in the admin panel. I'm thinking somewhere under settings/general maybe on the site tab. Create a document in /specs/toast-access that details options for where it should live and highlight a recommendation. You are an orchestrator. Use sub-agents as needed and consider using cheaper model for them. 
5. I reviewed the output in @specs/toast-access/toast-settings-location.md. I agree with the recommendation of a new tab. However think naming it Accessibility makes more sense.
6. I ran `/clear` and then prompted (Opus 5 High)
 > /spec using @specs/toast-access/toast-catalog.md and @specs/toast-access/toast-settings-location.md need to define a specification for this work. The new tab in the settings should be Accessibility. When the selection is 0 seconds need to indicate specifically that success toasts will not auto-dismiss. 
-  I considered using Fable 5.1 for the spec but have never used this workflow against it and it likely doesn't need it. I used this workflow since it documents my thinking.
- The skill asked me several questions with recommendations. Responded and for most of the questions I went with the recommendation. 
7. I reviewed the content of @.git/specs/toast-access/spec.md
8. I ran `/clear` and then `/plan` (Opus 5 High) to kick off the next step in my skill workflow.
- I answered a few questions
9. I (quickly) reviewed `.git/specs/toast-access/plan.md` and the `.git/specs/toast-access/tasks.md`
10. I ran `/clear` and then `/build` (Opus 5 High) to kick off the build phase which uses TDD.
- The agent informed me it was running the tasks sequentially. I had recently updated the skill so agents could run in parallel. I prompted the agent with `You may run more than 1 task at a time. I modified the skill but you must not have the most up to date copy`. The `5` models are much better at not clobbering each other. 
- Made an inflight decision to not localize the label for the "Accessibility" tab since the others were not localized.
- Claude started and E2E test which I watched but took forever (~15 min). There is a visible issue with the dismiss part of the toast but not enough time to address it. 
11. Since `/build` was held up by the lengthy E2E test (didn't want to kill), I started a new session and ran `/test` which confirms AC. The skills name needs to be updated. 
12. I wanted ~15 min to do a recording so effectively ended the run. I told the agents to get to a stopping point and record what they ahd done. 

## Remaining Work
I did some quick manual testing while the agent did automate E2E testing and things seemed to work as expected. As described in the specification there are actually 3 types of toast and only manually tested 2 of them. Given more time. I would have run the rest of my workflow which confirms AC and done more manual testing. This issue was larger in scope than the issue indicated. I thought it would fit well in a 2 hour window but the complexity put the total work just outside that. Another 30 minutes would be necessary. 

