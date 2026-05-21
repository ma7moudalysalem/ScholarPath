namespace ScholarPath.Infrastructure.Persistence.Seed;

/// <summary>
/// Realistic English + Arabic markdown bodies for the seeded
/// <c>Resource</c> rows. Kept in a dedicated file so the seeder code in
/// <c>DbSeeder.Resources.cs</c> stays focused on entity wiring rather than
/// long content blocks. Each pair (En/Ar) is hand-written, 150–300 words,
/// and uses literal markdown headings and list markers — no placeholders,
/// no lorem ipsum.
/// </summary>
internal static class ResourceBodies
{
    // ─────────────────────────────────────────────────────────────────────
    //                              GUIDES
    // ─────────────────────────────────────────────────────────────────────

    public const string CompleteGuideEn =
        "# The Complete Scholarship Application Guide\n\n" +
        "Applying for scholarships is a process you can systematise. This guide walks you through the four phases that turn a long list of possibilities into a focused, well-prepared application.\n\n" +
        "## 1. Research and shortlist\n\nStart broad: search by field, country and funding level. Save anything that looks plausible. After a week, revisit your saved list and rate each entry on three criteria — eligibility fit, deadline feasibility and effort to apply. Cut anything that scores low on more than one. You want eight to twelve serious targets, not sixty wishlist items.\n\n" +
        "## 2. Prepare your evidence\n\nGather transcripts, language test scores, identity documents and at least two recommendation letters. Get certified translations early; some consulates require originals stamped by a notary. A clean evidence folder also makes it easier to recycle materials between applications.\n\n" +
        "## 3. Write the personal statement\n\nThe personal statement is the only place reviewers hear your voice. Answer the prompt exactly, lead with a concrete moment, and explain why this scholarship — not a generic version of it — fits your trajectory. Aim for 700 to 900 words and read it aloud before sending.\n\n" +
        "## 4. Submit and follow up\n\nSubmit at least 48 hours before the deadline. After you submit, log the application in the tracker and set a reminder for the typical decision window. If you hear nothing past the published timeline, a single polite follow-up is appropriate.\n";

    public const string CompleteGuideAr =
        "# الدليل الكامل للتقديم على المنح الدراسية\n\n" +
        "التقديم على المنح عملية يمكنك تنظيمها. يأخذك هذا الدليل عبر أربع مراحل تحوّل قائمة طويلة من الفرص إلى طلب مركز ومُعد جيداً.\n\n" +
        "## 1. البحث وإعداد قائمة مختصرة\n\nابدأ بالبحث الواسع: ابحث حسب التخصص والدولة ومستوى التمويل. احفظ كل ما يبدو منطقياً. بعد أسبوع، راجع قائمتك المحفوظة وقيّم كل بند وفق ثلاثة معايير — الأهلية، إمكانية اللحاق بالموعد، والجهد المطلوب. احذف كل ما يحصل على درجة منخفضة في معيارين. تحتاج إلى ثمانية إلى اثني عشر هدفاً جاداً، لا ستين أمنية.\n\n" +
        "## 2. تجهيز الأدلة\n\nاجمع كشوف الدرجات، ودرجات اختبارات اللغة، ومستندات الهوية، وخطابين على الأقل من خطابات التوصية. احصل على ترجمات معتمدة مبكراً؛ بعض القنصليات تشترط أصولاً موثقة. مجلد أدلة منظم يسهّل أيضاً إعادة استخدام المواد بين الطلبات.\n\n" +
        "## 3. كتابة البيان الشخصي\n\nالبيان الشخصي هو المكان الوحيد الذي يسمع فيه المراجعون صوتك. أجب عن السؤال المطروح بدقة، وابدأ بلحظة ملموسة، وأوضح لماذا تناسبك هذه المنحة تحديداً. استهدف 700 إلى 900 كلمة واقرأ النص بصوت مرتفع قبل الإرسال.\n\n" +
        "## 4. التقديم والمتابعة\n\nقدّم قبل الموعد النهائي بـ48 ساعة على الأقل. بعد التقديم، سجّل الطلب في المتتبع واضبط تذكيراً بفترة القرار المعتادة. إذا لم تصلك ردود بعد انتهاء الجدول المعلن، يحق لك متابعة مهذبة واحدة.\n";

    public const string ShortlistGuideEn =
        "# Building a Scholarship Shortlist That Actually Fits You\n\n" +
        "Most applicants either apply to too many scholarships and burn out, or pick three at random and miss the ones they would have won. The shortlist is the bridge between exploring and applying.\n\n" +
        "## Score each opportunity\n\nFor every saved scholarship, score it from 1 to 5 on five dimensions: eligibility, funding amount, deadline feasibility, field fit and effort required. A score under 15 out of 25 is almost always a no.\n\n" +
        "## Cluster by deadline\n\nGroup what is left into three deadline buckets: this month, next month, and the long tail. The first bucket gets your focused effort. The third can wait — your stronger applications will inform them.\n\n" +
        "## Leave room for stretch and safety\n\nA balanced shortlist mixes one or two stretch applications (low odds, high reward), four to six fits (clear eligibility and a real match), and one or two safety choices (very high odds even if smaller funding). The safety choice exists so you have an offer to negotiate from.\n\n" +
        "## Decide what to drop\n\nWhen a new opportunity appears, do not just add it. Decide which existing target it replaces. A shortlist of more than twelve is no longer a shortlist.\n";

    public const string ShortlistGuideAr =
        "# بناء قائمة منح مختصرة تناسبك فعلاً\n\n" +
        "معظم المتقدمين إما يتقدمون لعدد كبير من المنح ويصابون بالإرهاق، أو يختارون ثلاثة منح عشوائياً ويفوّتون الفرص التي كانوا سيفوزون بها. القائمة المختصرة هي الجسر بين الاستكشاف والتقديم.\n\n" +
        "## قيّم كل فرصة\n\nلكل منحة محفوظة، قيّمها من 1 إلى 5 على خمسة محاور: الأهلية، مبلغ التمويل، إمكانية اللحاق بالموعد، ملاءمة التخصص، والجهد المطلوب. أي درجة تقل عن 15 من 25 هي تقريباً دائماً رفض.\n\n" +
        "## جمّع حسب المواعيد النهائية\n\nاقسم ما تبقى إلى ثلاث مجموعات زمنية: هذا الشهر، الشهر القادم، والذيل الطويل. المجموعة الأولى تأخذ تركيزك الكامل. المجموعة الثالثة يمكنها الانتظار — ستستفيد من طلباتك الأقوى.\n\n" +
        "## اترك مكاناً للطموح والأمان\n\nقائمة مختصرة متوازنة تجمع طلباً أو طلبين طموحين (احتمال منخفض ومكافأة عالية)، وأربعة إلى ستة طلبات ملائمة (أهلية واضحة وملاءمة حقيقية)، وطلباً أو طلبين آمنين (احتمال عالٍ جداً ولو بتمويل أصغر). الطلب الآمن يضمن لك عرضاً تفاوض من خلاله.\n\n" +
        "## قرّر ما تستبعده\n\nعندما تظهر فرصة جديدة، لا تضفها فقط. حدّد أي هدف قائم ستحلّ محله. القائمة المختصرة بأكثر من اثني عشر هدفاً لم تعد مختصرة.\n";

    public const string DocumentGuideEn =
        "# Document Preparation Playbook — Transcripts to Translations\n\n" +
        "The documentation phase is where most strong candidates lose time. Treat it as a project with sub-tasks, sequence them in the right order, and you will save weeks at the end.\n\n" +
        "## Order things by lead time\n\nDocuments that depend on third parties take the longest. Request an official transcript four to six weeks ahead — even if your university advertises a shorter turnaround. Recommendation letters take three to four weeks of polite reminders. Police clearance certificates can take six weeks. Start these first.\n\n" +
        "## Certify and translate\n\nWhere certified translation is required, use a translator on the embassy's approved list. Keep the original document, the translation, and a notarised copy of each together — committees and consulates frequently ask for the bundle.\n\n" +
        "## Build a master folder\n\nKeep one master folder on your computer with versioned PDFs: `transcript_v1_signed.pdf`, `passport_v2_with_renewal.pdf`. When you submit, copy from this folder rather than scanning fresh each time.\n\n" +
        "## Track expiry dates\n\nLanguage test certificates expire in two years, medical certificates in six months, police certificates in six to twelve months. Keep a small spreadsheet so you do not learn about an expired document the week before submission.\n";

    public const string DocumentGuideAr =
        "# دليل تجهيز المستندات — من كشف الدرجات إلى الترجمات\n\n" +
        "مرحلة المستندات هي التي يخسر فيها معظم المرشحين الأقوياء وقتهم. تعامل معها كمشروع مع مهام فرعية، ورتبها بالترتيب الصحيح، فستوفر أسابيع في النهاية.\n\n" +
        "## رتّب الأمور حسب الوقت المطلوب\n\nالمستندات التي تعتمد على طرف ثالث تستغرق وقتاً أطول. اطلب كشف درجات رسمياً قبل أربعة إلى ستة أسابيع — حتى لو أعلنت جامعتك مدة أقصر. خطابات التوصية تتطلب ثلاثة إلى أربعة أسابيع من التذكيرات المهذبة. شهادة السجل الجنائي قد تأخذ ستة أسابيع. ابدأ بهذه أولاً.\n\n" +
        "## التوثيق والترجمة\n\nحيث تشترط الترجمة المعتمدة، استخدم مترجماً مدرجاً على قائمة السفارة. احتفظ بالأصل والترجمة ونسخة موثقة من كل واحدة معاً — اللجان والقنصليات تطلب الحزمة كثيراً.\n\n" +
        "## أنشئ مجلداً رئيسياً\n\nأبقِ مجلداً واحداً على حاسوبك يحتوي PDFs بنسخ مرقمة: `transcript_v1_signed.pdf`، `passport_v2_with_renewal.pdf`. عند التقديم، انسخ من هذا المجلد بدلاً من المسح من جديد كل مرة.\n\n" +
        "## تابع تواريخ الانتهاء\n\nشهادات اختبارات اللغة تنتهي بعد سنتين، الشهادات الطبية بعد ستة أشهر، شهادات السجل الجنائي بعد ستة إلى اثني عشر شهراً. احتفظ بجدول صغير حتى لا تكتشف انتهاء مستند قبل أسبوع من التقديم.\n";

    public const string FundingGuideEn =
        "# Funding Your First Year — Budget, Sources and Backups\n\n" +
        "A scholarship rarely covers everything. Plan for the full cost of your first year and assemble several funding sources so you are not exposed if any one of them slips.\n\n" +
        "## Estimate the total\n\nList every cost in three buckets: tuition, mandatory fees (health insurance, registration, visa) and living costs (rent, food, transport, books). Add 10% buffer for currency movement.\n\n" +
        "## Stack your sources\n\nA realistic first-year budget often combines: a primary scholarship, a department-level tuition waiver, a small national grant, family contribution, and 8 to 12 hours per week of work-study allowed by your visa. Stacking is normal and expected.\n\n" +
        "## Build a three-month cushion\n\nScholarship stipends arrive late. Many students wait six to ten weeks for the first payment. Plan for at least three months of expenses in personal savings or family bridging — and confirm that figure with the international office before you fly.\n\n" +
        "## Have a backup\n\nIdentify in advance which expense you would cut first if a source fell through — usually the optional language course or the larger apartment. Knowing this in October prevents a panic in February.\n";

    public const string FundingGuideAr =
        "# تمويل سنتك الأولى — الميزانية والمصادر والاحتياطيات\n\n" +
        "نادراً ما تغطي المنحة كل شيء. خطط للتكلفة الكاملة لسنتك الأولى واجمع عدة مصادر تمويل حتى لا تكون مكشوفاً إذا تأخر أي منها.\n\n" +
        "## احسب الإجمالي\n\nاكتب كل تكلفة في ثلاث فئات: الرسوم الدراسية، الرسوم الإلزامية (التأمين الصحي، التسجيل، التأشيرة)، وتكاليف المعيشة (الإيجار، الطعام، المواصلات، الكتب). أضف هامش 10% لتقلبات العملة.\n\n" +
        "## اجمع مصادرك\n\nميزانية السنة الأولى الواقعية تجمع غالباً: منحة أساسية، وإعفاء رسوم من القسم، ومنحة وطنية صغيرة، ومساهمة عائلية، و8 إلى 12 ساعة عمل أسبوعياً تسمح بها تأشيرتك. الجمع طبيعي ومتوقع.\n\n" +
        "## ابنِ احتياطياً لثلاثة أشهر\n\nرواتب المنح تصل متأخرة. ينتظر كثير من الطلاب ستة إلى عشرة أسابيع للدفعة الأولى. خطط لمصاريف ثلاثة أشهر على الأقل من مدخرات شخصية أو دعم عائلي مؤقت — وأكّد هذا الرقم مع مكتب الشؤون الدولية قبل السفر.\n\n" +
        "## احتفظ بخطة بديلة\n\nحدد مسبقاً أي نفقة ستقطعها أولاً إذا تعثر أحد المصادر — عادة دورة اللغة الاختيارية أو الشقة الأكبر. معرفة هذا في أكتوبر تمنع حالة الذعر في فبراير.\n";

    public const string VisaGuideEn =
        "# Visa Application Step-By-Step — From Acceptance to Boarding\n\n" +
        "Working backwards from your flight date is the only way to avoid a panicked visa application. This guide assumes a roughly twelve-week window.\n\n" +
        "## Week 12 to 10 — Confirm and book\n\nAccept your offer, pay any deposits, and book a tentative appointment slot at the consulate. Some consulates open appointments three months in advance; popular ones fill within hours.\n\n" +
        "## Week 10 to 6 — Documents\n\nStart your financial-evidence trail by transferring sponsorship funds into the named account that will hold them for the 28-day rule. Order police clearance and gather your acceptance letter, transcripts and proof of language. If a tuberculosis test is required by your destination, schedule it now.\n\n" +
        "## Week 6 to 3 — Submit\n\nFill in the online form, upload supporting documents and pay the fee. Print the appointment confirmation. Verify every field on the printed form against your passport spelling.\n\n" +
        "## Week 3 to 1 — Interview\n\nDress neatly, arrive thirty minutes early and bring originals of every document you uploaded. Be honest about your plans after graduation; consular officers test for genuine intent to study.\n\n" +
        "## Week 1 — Pre-flight\n\nNotify your bank you will be abroad, register with your embassy if recommended, and confirm your housing arrival window with the landlord or university.\n";

    public const string VisaGuideAr =
        "# خطوات طلب التأشيرة — من القبول إلى الصعود للطائرة\n\n" +
        "العمل من تاريخ رحلتك إلى الوراء هو الطريقة الوحيدة لتجنب طلب تأشيرة مرتبك. يفترض هذا الدليل نافذة زمنية حوالي اثني عشر أسبوعاً.\n\n" +
        "## الأسبوع 12 إلى 10 — التأكيد والحجز\n\nاقبل عرض القبول، وادفع أي عربون، واحجز موعداً مبدئياً في القنصلية. بعض القنصليات تفتح المواعيد قبل ثلاثة أشهر؛ المواعيد الشعبية تمتلئ خلال ساعات.\n\n" +
        "## الأسبوع 10 إلى 6 — المستندات\n\nابدأ مسار الإثبات المالي بتحويل أموال الكفالة إلى الحساب المسجل الذي سيحتفظ بها لقاعدة 28 يوماً. اطلب شهادة السجل الجنائي واجمع خطاب القبول وكشوف الدرجات وإثبات اللغة. إذا طلبت دولة الوجهة فحص السل، حدد له موعداً الآن.\n\n" +
        "## الأسبوع 6 إلى 3 — التقديم\n\nاملأ النموذج الإلكتروني، وارفع المستندات الداعمة، وادفع الرسوم. اطبع تأكيد الموعد. تحقق من كل خانة في النموذج المطبوع مقابل تهجئة جواز السفر.\n\n" +
        "## الأسبوع 3 إلى 1 — المقابلة\n\nالبس أنيقاً، احضر قبل ثلاثين دقيقة، وأحضر أصول كل مستند رفعته. كن صادقاً بشأن خططك بعد التخرج؛ يختبر ضباط القنصلية نية الدراسة الحقيقية.\n\n" +
        "## الأسبوع الأخير — قبل السفر\n\nأبلغ بنكك بسفرك، وسجّل في سفارتك إذا كان موصى به، وأكّد نافذة الوصول للسكن مع المالك أو الجامعة.\n";

    public const string PreDepartureEn =
        "# Pre-Departure Guide — The Last Two Weeks Before You Fly\n\n" +
        "By the time the last two weeks arrive, your visa is in hand and your housing is booked. These fourteen days are about logistics, money and paperwork that you can only finish near the date.\n\n" +
        "## Money and banking\n\nNotify your home bank of foreign use to avoid your card being blocked at arrival. Move a small starter sum to an international debit card or travel-friendly bank. Cash for the first two days is sensible; large cash sums in your luggage are not.\n\n" +
        "## Insurance and health\n\nConfirm your travel insurance covers the gap before your destination health insurance starts. Pick up any prescriptions for at least sixty days, and carry a doctor's note for medication.\n\n" +
        "## Paperwork\n\nMake digital and paper copies of your passport, visa, acceptance letter, housing contract and insurance. Keep one paper copy in your carry-on and another in your checked bag. Email a copy to yourself.\n\n" +
        "## Packing\n\nPack for the first three weeks, not the year. You will buy household items locally. Save baggage allowance for items genuinely cheaper at home — favourite spices, an extra pair of glasses, comfortable shoes.\n\n" +
        "## Goodbye logistics\n\nPlan a small farewell with family rather than a big party the night before — you want to land rested. Confirm your airport transfer.\n";

    public const string PreDepartureAr =
        "# دليل ما قبل السفر — الأسبوعان الأخيران قبل الطيران\n\n" +
        "حين يحل الأسبوعان الأخيران، تكون التأشيرة بيدك والسكن محجوزاً. هذه الأيام الأربعة عشر مخصصة للمسائل اللوجستية والمالية والورقية التي لا تكتمل إلا قرب الموعد.\n\n" +
        "## النقود والبنوك\n\nأبلغ بنكك المحلي باستخدامك خارجياً لتجنب تجميد بطاقتك عند الوصول. حوّل مبلغاً صغيراً للبدء إلى بطاقة دولية أو بنك ملائم للسفر. حمل نقداً ليومين أولين أمر معقول؛ مبالغ نقدية كبيرة في الحقيبة ليست كذلك.\n\n" +
        "## التأمين والصحة\n\nأكّد أن تأمين السفر يغطي الفجوة قبل بدء تأمينك الصحي في الوجهة. احصل على وصفاتك الدوائية لمدة ستين يوماً على الأقل، واحمل ملاحظة من طبيب للأدوية.\n\n" +
        "## الأوراق\n\nاحتفظ بنسخ رقمية وورقية من جواز سفرك وتأشيرتك وخطاب القبول وعقد السكن والتأمين. ضع نسخة ورقية في حقيبة اليد وأخرى في الحقيبة المسجلة. أرسل نسخة إلى بريدك الإلكتروني.\n\n" +
        "## التعبئة\n\nاحزم لثلاثة أسابيع أولى، لا لسنة كاملة. ستشتري الأدوات المنزلية محلياً. وفّر سعة الحقيبة لأشياء أرخص فعلاً في بلدك — توابلك المفضلة، نظارة احتياطية، حذاء مريح.\n\n" +
        "## ترتيبات الوداع\n\nخطط لوداع عائلي صغير بدلاً من حفلة كبيرة ليلة السفر — تريد الوصول مرتاحاً. أكد ترتيبات النقل من المطار.\n";

    public const string BudgetingGuideEn =
        "# Budgeting for Life as an International Student\n\n" +
        "Track rent, food, transport and insurance from day one. The students who run out of money in their second semester rarely overspent on something obvious — they were not tracking at all.\n\n" +
        "## Separate fixed and variable costs\n\nRent, insurance, transit pass and phone plan are fixed; you cannot reduce them this month. Food, leisure, books and travel are variable; this is where the dial is. A simple rule: fixed costs should never exceed 65% of your monthly income.\n\n" +
        "## Watch the first three months\n\nThe first three months are skewed. You buy household goods, sign deposits and pay one-time fees. Do not assume that pattern continues. Re-estimate your monthly base in month four.\n\n" +
        "## Build an emergency fund\n\nAim for one month of expenses in reserve by the end of your first semester. Add to it monthly until it covers three months. This is the cushion that lets you handle a delayed stipend or a surprise dentist bill without panic.\n\n" +
        "## Use the right tools\n\nA single spreadsheet or budgeting app is more useful than five. The best budget is the one you actually open every week.\n";

    public const string BudgetingGuideAr =
        "# إعداد الميزانية للحياة كطالب دولي\n\n" +
        "تابع الإيجار والطعام والمواصلات والتأمين من اليوم الأول. الطلاب الذين تنفد أموالهم في الفصل الثاني نادراً ما أنفقوا على شيء واضح بشكل مفرط — هم لم يتابعوا أساساً.\n\n" +
        "## افصل التكاليف الثابتة عن المتغيرة\n\nالإيجار والتأمين وبطاقة المواصلات وخطة الهاتف ثابتة؛ لا يمكنك تقليلها هذا الشهر. الطعام والترفيه والكتب والسفر متغيرة؛ هنا المسار قابل للضبط. قاعدة بسيطة: لا ينبغي أن تتجاوز التكاليف الثابتة 65% من دخلك الشهري.\n\n" +
        "## راقب الأشهر الثلاثة الأولى\n\nالأشهر الثلاثة الأولى منحرفة. تشتري أدوات منزلية، وتدفع تأمينات، ورسوماً لمرة واحدة. لا تفترض استمرار هذا النمط. أعد تقدير قاعدتك الشهرية في الشهر الرابع.\n\n" +
        "## ابنِ صندوق طوارئ\n\nاستهدف ادخار نفقات شهر كاحتياطي بنهاية فصلك الأول. أضف إليه شهرياً حتى يغطي ثلاثة أشهر. هذا الاحتياطي يسمح لك بالتعامل مع تأخر راتب أو فاتورة طبيب أسنان مفاجئة دون ذعر.\n\n" +
        "## استخدم الأدوات المناسبة\n\nجدول واحد أو تطبيق ميزانية أنفع من خمسة. أفضل ميزانية هي التي تفتحها فعلاً كل أسبوع.\n";

    // ─────────────────────────────────────────────────────────────────────
    //                              ARTICLES
    // ─────────────────────────────────────────────────────────────────────

    public const string FiveMistakesEn =
        "# Five Mistakes That Sink Scholarship Essays\n\n" +
        "Reviewers read hundreds of essays per cycle. The strong ones stand out; the rejected ones tend to make the same five mistakes.\n\n" +
        "## 1. Generic openings\n\nA scholarship essay that begins with \"Education has always been important to me\" is indistinguishable from a thousand others. Open with a specific moment — a project, a conversation, a place — and the reviewer pays attention.\n\n" +
        "## 2. Listing instead of storytelling\n\nA list of clubs and grades does not show character. A short story about a single moment from one of those clubs does. Cut accomplishments from your essay and put them in your CV where they belong.\n\n" +
        "## 3. Ignoring the prompt\n\nSelection panels score against the prompt, not against your full life story. If the prompt asks why you chose this field, ninety per cent of the essay should be about that choice — not your school years.\n\n" +
        "## 4. Weak conclusions\n\nA closing paragraph that restates the opening wastes the most memorable position in the essay. End with a forward-looking line that connects your goals to the scholarship's mission.\n\n" +
        "## 5. Skipping proofreading\n\nTypos signal carelessness. Read your essay aloud, ask one person from outside your field to read it, and run a spelling pass on the final version.\n";

    public const string FiveMistakesAr =
        "# خمسة أخطاء تُفشل مقالات المنح الدراسية\n\n" +
        "يقرأ المراجعون مئات المقالات لكل دورة. المقالات القوية تبرز؛ المقالات المرفوضة تكرر الأخطاء الخمسة نفسها.\n\n" +
        "## 1. مقدمات عامة\n\nمقال منحة يبدأ بـ\"كان التعليم دائماً مهماً بالنسبة لي\" لا يختلف عن ألف مقال آخر. ابدأ بلحظة محددة — مشروع، محادثة، مكان — يهتم المراجع.\n\n" +
        "## 2. السرد على شكل قوائم\n\nقائمة من الأندية والدرجات لا تُظهر شخصية. قصة قصيرة عن لحظة واحدة من أحد تلك الأندية تفعل ذلك. اقتطع الإنجازات من مقالك وضعها في سيرتك الذاتية حيث ينتمي مكانها.\n\n" +
        "## 3. تجاهل السؤال\n\nلجان الاختيار تقيّم وفق السؤال المطروح، لا قصة حياتك بأكملها. إذا كان السؤال عن سبب اختيار هذا التخصص، فتسعون بالمئة من المقال يجب أن يدور حول ذلك الاختيار — لا سنوات المدرسة.\n\n" +
        "## 4. خواتيم ضعيفة\n\nفقرة ختامية تعيد صياغة المقدمة تُهدر أكثر مواقع المقال إثارة للذكرى. اختم بسطر يتطلع للأمام يربط أهدافك برسالة المنحة.\n\n" +
        "## 5. إهمال المراجعة اللغوية\n\nالأخطاء الإملائية تشير إلى لا مبالاة. اقرأ مقالك بصوت مرتفع، واطلب من شخص من خارج تخصصك أن يقرأه، وقم بفحص تدقيق إملائي للنسخة النهائية.\n";

    public const string SopEn =
        "# Statement of Purpose: A Practical Writing Guide\n\n" +
        "A statement of purpose is not a personal essay. It is an argument for why a specific committee, at a specific institution, should invest in your specific plan.\n\n" +
        "## Plan before you write\n\nIn one paragraph, answer four questions: What do you want to study? Why this field? Why this institution? What will you do after? If you cannot answer any of these in a sentence, the essay will be vague.\n\n" +
        "## Start with a moment\n\nThe first paragraph should ground the reader in a specific image — a lab, a classroom, a community project. From that moment, fan out to the wider trajectory.\n\n" +
        "## Connect, do not list\n\nEvery experience you mention must connect to the next. \"I built this app, which taught me X, which made me curious about Y, which is why this programme is the right next step.\" If you cannot draw the connection, leave the experience out.\n\n" +
        "## Close on a plan\n\nThe last paragraph should describe what you intend to do during the programme and after it. Reviewers are funding a future trajectory, not a past CV. Show them where their investment leads.\n\n" +
        "## Polish\n\nAim for 700 to 900 words. Read it aloud, cut anything that does not directly answer the prompt, and ask a reader unfamiliar with your field to flag jargon.\n";

    public const string SopAr =
        "# خطاب الغرض: دليل كتابة عملي\n\n" +
        "خطاب الغرض ليس مقالاً شخصياً. إنه حجة على سبب ضرورة استثمار لجنة محددة، في مؤسسة محددة، في خطتك المحددة.\n\n" +
        "## خطط قبل الكتابة\n\nفي فقرة واحدة، أجب عن أربعة أسئلة: ماذا تريد أن تدرس؟ لماذا هذا التخصص؟ لماذا هذه المؤسسة؟ ماذا ستفعل بعدها؟ إذا لم تستطع الإجابة عن أي منها في جملة، فسيكون المقال مبهماً.\n\n" +
        "## ابدأ بلحظة\n\nيجب أن تثبت الفقرة الأولى القارئ في مشهد محدد — مختبر، فصل دراسي، مشروع مجتمعي. من تلك اللحظة، اتسع إلى المسار الأوسع.\n\n" +
        "## اربط ولا تعدّد\n\nكل تجربة تذكرها يجب أن تتصل بالتالية. \"بنيت هذا التطبيق، الذي علمني س، الذي جعلني فضولياً بشأن ص، ولهذا هذا البرنامج هو الخطوة الصحيحة التالية.\" إذا لم تستطع رسم الرابط، اترك التجربة.\n\n" +
        "## اختم بخطة\n\nيجب أن تصف الفقرة الأخيرة ما تنوي فعله خلال البرنامج وبعده. المراجعون يموّلون مسار مستقبل، لا سيرة ماضٍ. أرهم إلى أين يقود استثمارهم.\n\n" +
        "## الصقل\n\nاستهدف 700 إلى 900 كلمة. اقرأه بصوت مرتفع، واقتطع كل ما لا يجيب مباشرة عن السؤال، واطلب من قارئ غير متخصص في مجالك أن يشير إلى أي مصطلحات معقدة.\n";

    public const string RecommendationEn =
        "# Recommendation Letters: Asking and Following Up\n\n" +
        "Strong recommendation letters take six weeks to set up and ten minutes per request to fail. The difference is in how you ask.\n\n" +
        "## Who to ask\n\nPick people who taught you, supervised you or worked with you closely. Title matters less than specificity. A junior lecturer who saw you lead a class project writes a more credible letter than a famous professor who saw you in a lecture of 200.\n\n" +
        "## What to send\n\nAlong with the formal request, send a short brief: which scholarship, what the deadline is, why you are applying, your CV, a draft of your statement of purpose, and two or three points you hope the letter could touch on. Make it easy to write a strong letter about you.\n\n" +
        "## When to ask\n\nAsk six weeks before the earliest deadline. If the deadline is closer, acknowledge it directly — do not pretend you have more time.\n\n" +
        "## Following up\n\nOne polite reminder ten days before the deadline is appropriate. A second reminder seventy-two hours out is also reasonable. Beyond that you risk the letter being written hastily, which is worse than late.\n\n" +
        "## After they submit\n\nThank your referee in writing and tell them the outcome — accepted, rejected, deferred. They remember candidates who close the loop.\n";

    public const string RecommendationAr =
        "# خطابات التوصية: كيف تطلبها وتتابعها\n\n" +
        "خطابات التوصية القوية تستغرق ستة أسابيع لإعدادها وعشر دقائق لكل طلب لتفشل. الفرق في كيف تطلبها.\n\n" +
        "## ممن تطلب\n\nاختر أشخاصاً علّموك، أو أشرفوا عليك، أو عملوا معك عن قرب. اللقب أقل أهمية من التحديد. محاضر مبتدئ شاهدك تقود مشروعاً صفياً يكتب خطاباً أكثر مصداقية من أستاذ شهير شاهدك في محاضرة من 200 طالب.\n\n" +
        "## ماذا ترسل\n\nمع الطلب الرسمي، أرسل ملخصاً قصيراً: أي منحة، ما الموعد النهائي، لماذا تتقدم، سيرتك الذاتية، مسودة خطاب الغرض، ونقطتين أو ثلاث تأمل أن يتطرق إليها الخطاب. اجعل كتابة خطاب قوي عنك سهلة.\n\n" +
        "## متى تطلب\n\nاطلب قبل أبكر موعد نهائي بستة أسابيع. إذا كان الموعد أقرب، اعترف بذلك مباشرة — لا تتظاهر بأن أمامك وقتاً أطول.\n\n" +
        "## المتابعة\n\nتذكير مهذب واحد قبل الموعد النهائي بعشرة أيام مناسب. تذكير ثانٍ قبل اثنتين وسبعين ساعة معقول أيضاً. ما بعد ذلك تخاطر بكتابة الخطاب على عجل، وهو أسوأ من المتأخر.\n\n" +
        "## بعد التقديم\n\nاشكر معطي التوصية كتابياً وأخبره بالنتيجة — قُبلت، رُفضت، أُجّلت. هم يتذكرون المرشحين الذين يُغلقون الدائرة.\n";

    public const string InterviewEn =
        "# Interview Preparation: What Selection Panels Look For\n\n" +
        "A scholarship interview is not a memory test. The panel wants to confirm three things: the application is genuinely yours, your plan is realistic, and you will represent the programme well.\n\n" +
        "## Read your own application first\n\nThe most common reason candidates stumble is forgetting what they wrote. Re-read your statement, your CV and your project descriptions the night before. Be ready to discuss any phrase in detail.\n\n" +
        "## Prepare for three categories\n\nMost questions fall into one of three categories: motivation (why this field, why this programme), capability (give me an example when you handled X), and plans (what will you do during and after). Prepare two specific stories you can adapt to several questions.\n\n" +
        "## Tell stories, not lists\n\nWhen asked about leadership, do not list club roles. Pick one role, describe one decision and what you learned. Specific stories are more memorable than complete CVs.\n\n" +
        "## Stay on-message\n\nFor every answer, ask yourself: does this strengthen my case for the scholarship? If not, redirect briefly and finish on something that does.\n\n" +
        "## Have questions ready\n\nThe panel will ask if you have questions. \"I don't have any\" is a missed opportunity. Have two prepared — one about the programme, one about cohort experience.\n";

    public const string InterviewAr =
        "# التحضير للمقابلة: ما تبحث عنه لجان الاختيار\n\n" +
        "مقابلة المنحة ليست اختبار ذاكرة. تريد اللجنة التأكد من ثلاثة أمور: أن الطلب حقاً ملكك، أن خطتك واقعية، وأنك ستمثل البرنامج جيداً.\n\n" +
        "## اقرأ طلبك أولاً\n\nأكثر سبب يعثر المرشحون لأجله هو نسيان ما كتبوه. أعد قراءة بيانك وسيرتك الذاتية ووصف مشاريعك في الليلة السابقة. كن مستعداً لمناقشة أي عبارة بالتفصيل.\n\n" +
        "## استعد لثلاث فئات\n\nمعظم الأسئلة تقع في إحدى ثلاث فئات: الدافع (لماذا هذا التخصص، لماذا هذا البرنامج)، القدرة (أعطني مثالاً تعاملت فيه مع س)، والخطط (ماذا ستفعل خلال البرنامج وبعده). جهّز قصتين محددتين يمكنك تكييفهما لعدة أسئلة.\n\n" +
        "## احكِ قصصاً لا قوائم\n\nحين تُسأل عن القيادة، لا تعدد أدواراً في أندية. اختر دوراً واحداً، صف قراراً واحداً وما تعلمته منه. القصص المحددة تبقى في الذاكرة أكثر من السير الذاتية الكاملة.\n\n" +
        "## ابقَ ضمن رسالتك\n\nلكل إجابة، اسأل نفسك: هل يقوي هذا حجتي للمنحة؟ إن لم يكن، حوّل الموضوع باختصار واختم بشيء يقويها.\n\n" +
        "## جهّز أسئلة\n\nستسألك اللجنة إن كانت لديك أسئلة. \"ليس لدي\" فرصة ضائعة. جهز اثنين — واحد عن البرنامج، وآخر عن تجربة المجموعة.\n";

    public const string IeltsEn =
        "# IELTS Strategy: Score-Boosting Tactics for Each Section\n\n" +
        "A 7.0 overall is the realistic ceiling for most candidates after six to eight weeks of focused study. Pushing higher requires section-by-section tactics.\n\n" +
        "## Listening\n\nThe biggest gains come from spelling. Half of the marks lost on Listening are misspellings, not mishearings. Practise common academic vocabulary spellings until they are automatic.\n\n" +
        "## Reading\n\nDo not read every word. Skim the questions first, then scan the passage for matching keywords. Mark your answer sheet in pencil as you go — losing time at the transfer stage is a common avoidable mistake.\n\n" +
        "## Writing\n\nTask 2 carries more weight than Task 1. Spend forty minutes on Task 2, leaving twenty for Task 1. Memorise three structures (agree/disagree, both views, problem/solution) and a small bank of linking phrases.\n\n" +
        "## Speaking\n\nExaminers reward extended answers in Part 1. \"Yes, I do\" earns nothing. Aim for two or three sentences per response. In Part 3, take a breath before answering — pauses are fine and let you organise your point.\n\n" +
        "## Mock tests\n\nDo two timed full mocks per week in the final three weeks. Score them honestly. The pattern of mistakes matters more than the band score on any one mock.\n";

    public const string IeltsAr =
        "# استراتيجية الآيلتس: تكتيكات لرفع الدرجة في كل قسم\n\n" +
        "درجة 7.0 إجمالية هي السقف الواقعي لمعظم المرشحين بعد ستة إلى ثمانية أسابيع من الدراسة المركزة. الدفع لأعلى يتطلب تكتيكات قسم-قسم.\n\n" +
        "## الاستماع\n\nأكبر مكاسب تأتي من التهجئة. نصف الدرجات المفقودة في الاستماع أخطاء تهجئة، لا أخطاء سماع. درب نفسك على تهجئة المفردات الأكاديمية الشائعة حتى تصبح تلقائية.\n\n" +
        "## القراءة\n\nلا تقرأ كل كلمة. تصفّح الأسئلة أولاً، ثم امسح النص بحثاً عن كلمات مفتاحية مطابقة. حدد إجاباتك بالقلم الرصاص أثناء العمل — فقد الوقت في مرحلة النقل خطأ شائع يمكن تجنبه.\n\n" +
        "## الكتابة\n\nالمهمة الثانية أثقل وزناً من الأولى. اقضِ أربعين دقيقة على المهمة الثانية، واترك عشرين للأولى. احفظ ثلاث هياكل (موافق/معارض، كلا الرأيين، مشكلة/حل) ومجموعة صغيرة من عبارات الربط.\n\n" +
        "## المحادثة\n\nيكافئ الممتحنون الإجابات الممتدة في الجزء الأول. \"نعم، أفعل\" لا يكسب شيئاً. استهدف جملتين أو ثلاثاً لكل رد. في الجزء الثالث، خذ نفساً قبل الإجابة — الوقفات مقبولة وتمنحك تنظيم النقطة.\n\n" +
        "## الاختبارات التجريبية\n\nأجرِ اختبارين كاملين بتوقيت في الأسابيع الثلاثة الأخيرة كل أسبوع. قيّمها بصدق. نمط الأخطاء أهم من درجة الباند في أي اختبار واحد.\n";

    public const string ToeflEn =
        "# TOEFL Preparation in 30 Days\n\n" +
        "Thirty days is enough to gain four to six points if you start at a 90 baseline and study about two hours a day. The plan below assumes that.\n\n" +
        "## Days 1 to 5 — Baseline\n\nTake a free full mock test. Identify your weakest section. Do not jump to study materials yet; the first five days are diagnostic.\n\n" +
        "## Days 6 to 15 — Build the weak section\n\nSpend an hour a day on the weakest section: integrated tasks for Writing, note-taking discipline for Listening, paraphrasing for Speaking, or strategy passages for Reading. The other hour rotates the remaining three.\n\n" +
        "## Days 16 to 25 — Integrated practice\n\nMix sections daily. Real tests rotate skills quickly; your brain needs to be ready to switch. Two full skills per day, alternating.\n\n" +
        "## Days 26 to 28 — Full mocks\n\nThree complete timed mock tests on consecutive days. Score them and review each error. Patterns at this stage tell you what to fix in the final week.\n\n" +
        "## Days 29 to 30 — Calm and review\n\nNo new content. Light review of templates, vocabulary lists and timing rules. Get a full night's sleep before the exam — well-rested candidates outperform tired ones by a meaningful margin.\n";

    public const string ToeflAr =
        "# التحضير لاختبار التوفل في 30 يوماً\n\n" +
        "ثلاثون يوماً تكفي لكسب أربع إلى ست درجات إذا بدأت من قاعدة 90 ودرست حوالي ساعتين يومياً. تفترض الخطة أدناه ذلك.\n\n" +
        "## الأيام 1 إلى 5 — القياس\n\nأجرِ اختباراً تجريبياً كاملاً مجانياً. حدد قسمك الأضعف. لا تقفز إلى مواد الدراسة بعد؛ الأيام الخمسة الأولى تشخيصية.\n\n" +
        "## الأيام 6 إلى 15 — تقوية القسم الضعيف\n\nاقضِ ساعة يومياً على القسم الأضعف: المهام المتكاملة للكتابة، انضباط التدوين للاستماع، إعادة الصياغة للمحادثة، أو استراتيجية النصوص للقراءة. الساعة الأخرى تتناوب بين الأقسام الثلاثة المتبقية.\n\n" +
        "## الأيام 16 إلى 25 — التدريب المتكامل\n\nاخلط الأقسام يومياً. الاختبارات الحقيقية تتنقل بين المهارات بسرعة؛ يحتاج دماغك أن يكون مستعداً للتبديل. مهارتان كاملتان يومياً بالتناوب.\n\n" +
        "## الأيام 26 إلى 28 — اختبارات تجريبية كاملة\n\nثلاثة اختبارات تجريبية كاملة بتوقيت في أيام متتالية. قيّمها وراجع كل خطأ. الأنماط في هذه المرحلة تخبرك ما يجب إصلاحه في الأسبوع الأخير.\n\n" +
        "## اليومان 29 و30 — هدوء ومراجعة\n\nلا محتوى جديد. مراجعة خفيفة للقوالب وقوائم المفردات وقواعد التوقيت. احصل على نوم كامل قبل الامتحان — المرشحون المرتاحون يتفوقون على المتعبين بهامش ملحوظ.\n";

    public const string FinAidEn =
        "# Financial Aid Options Beyond the Headline Scholarship\n\n" +
        "Most students chase the same three or four scholarship programmes. The ones who actually fund their studies stack smaller awards on top of a main offer.\n\n" +
        "## Tuition waivers\n\nMany universities advertise scholarships but quietly grant tuition waivers to admitted international students who ask. The financial aid office is the right place to ask, not the admissions office.\n\n" +
        "## Departmental grants\n\nAcademic departments often have small grants — five hundred to three thousand dollars — that never appear on the main scholarship page. They are funded by alumni and discretionary budgets. Email the department graduate coordinator directly.\n\n" +
        "## Work-study programmes\n\nMost student visas allow part-time work on campus, usually capped at 20 hours per week. On-campus jobs pay modestly but are tax-friendly and let you stay in academic networks.\n\n" +
        "## Emergency funds\n\nVirtually every university has a discretionary emergency fund for students who hit unexpected costs — a delayed scholarship, a family emergency, a sudden medical bill. They are usually grants, not loans. Knowing who to email matters; finding out in the middle of a crisis is hard.\n\n" +
        "## Country-specific schemes\n\nSeveral countries run subsidies for foreign students that are not labelled scholarships — transit discounts, health subsidies, museum passes. They add up.\n";

    public const string FinAidAr =
        "# خيارات المساعدة المالية بخلاف المنح الرئيسية\n\n" +
        "معظم الطلاب يلحقون بنفس الثلاث أو الأربع برامج منح. الذين يموّلون دراستهم فعلاً يجمعون مكافآت أصغر فوق العرض الرئيسي.\n\n" +
        "## إعفاءات الرسوم\n\nكثير من الجامعات تعلن عن منح لكنها تمنح بهدوء إعفاءات رسوم للطلاب الدوليين المقبولين الذين يطلبون. مكتب المساعدة المالية هو المكان الصحيح للسؤال، لا مكتب القبول.\n\n" +
        "## المنح الداخلية للأقسام\n\nالأقسام الأكاديمية غالباً ما تملك منحاً صغيرة — من خمسمئة إلى ثلاثة آلاف دولار — لا تظهر أبداً في صفحة المنح الرئيسية. تمولها الميزانيات التقديرية والخريجون. أرسل بريداً مباشراً إلى منسق الدراسات العليا في القسم.\n\n" +
        "## برامج العمل-الدراسة\n\nمعظم تأشيرات الطلاب تسمح بعمل جزئي داخل الحرم الجامعي، عادة بحد أقصى 20 ساعة أسبوعياً. وظائف الحرم تدفع باعتدال لكنها صديقة ضريبياً وتُبقيك في الشبكات الأكاديمية.\n\n" +
        "## صناديق الطوارئ\n\nكل جامعة تقريباً تملك صندوق طوارئ تقديرياً للطلاب الذين يواجهون تكاليف غير متوقعة — منحة متأخرة، طارئ عائلي، فاتورة طبية مفاجئة. عادةً ما تكون منحاً لا قروضاً. معرفة من تراسل أمر مهم؛ اكتشافه في منتصف أزمة صعب.\n\n" +
        "## برامج خاصة بالدولة\n\nعدة دول تدير دعماً للطلاب الأجانب لا يُسمى منحاً — خصومات مواصلات، دعم صحي، تذاكر متاحف. يتراكم.\n";

    public const string CulturalEn =
        "# Cultural Adaptation Abroad — The First 90 Days\n\n" +
        "The first month abroad usually feels exciting. The second can feel hard. The third is when most students figure out how to actually live in the new place.\n\n" +
        "## Weeks 1 to 4 — Honeymoon\n\nEverything is new. Coffee tastes different, public transport runs on a different rhythm, even shop hours feel exotic. Enjoy this. Do not over-commit your social calendar; you will need bandwidth later.\n\n" +
        "## Weeks 5 to 8 — Friction\n\nThe new feels normal but you still don't have your routines. You miss home food, you don't know which doctor to call, you cannot find a familiar brand. This is when most students feel low. It is also the moment to invest in one or two friendships that go beyond classmates.\n\n" +
        "## Weeks 9 to 12 — Integration\n\nYou know the bus you take, the grocery store you trust, the people you can ask for help. Cultural friction does not disappear — it just becomes manageable.\n\n" +
        "## Tactics that help\n\nKeep one strong link home (a weekly video call), but resist constant connection. Cook with a classmate. Take one class outside your degree (language, art, sports) to widen your circle. Walk one new street every week.\n\n" +
        "## When to seek support\n\nPersistent low mood beyond week ten is not normal homesickness. University counselling is usually free and confidential — use it.\n";

    public const string CulturalAr =
        "# التكيف الثقافي في الخارج — أول 90 يوماً\n\n" +
        "الشهر الأول في الخارج يبدو مثيراً عادةً. الثاني قد يبدو صعباً. الثالث هو حين يكتشف معظم الطلاب كيف يعيشون فعلاً في المكان الجديد.\n\n" +
        "## الأسابيع 1 إلى 4 — شهر العسل\n\nكل شيء جديد. القهوة طعمها مختلف، المواصلات العامة لها إيقاع مختلف، حتى ساعات المتاجر تبدو غريبة. استمتع بهذا. لا تحجز جدولك الاجتماعي بإفراط؛ ستحتاج سعة لاحقاً.\n\n" +
        "## الأسابيع 5 إلى 8 — الاحتكاك\n\nالجديد أصبح عادياً لكنك ما زلت لا تملك روتيناتك. تشتاق لطعام البلد، لا تعرف أي طبيب تتصل به، لا تجد ماركة مألوفة. هذا حين يشعر معظم الطلاب بالإحباط. وهي اللحظة التي تستثمر فيها في صداقة أو اثنتين تتجاوزان زملاء الفصل.\n\n" +
        "## الأسابيع 9 إلى 12 — الاندماج\n\nتعرف الباص الذي تأخذه، والبقالة التي تثق بها، والأشخاص الذين تسألهم المساعدة. الاحتكاك الثقافي لا يختفي — فقط يصبح قابلاً للإدارة.\n\n" +
        "## تكتيكات تساعد\n\nأبقِ على رابط قوي واحد مع الوطن (مكالمة فيديو أسبوعية)، لكن قاوم الاتصال المستمر. اطبخ مع زميل. خذ صفاً واحداً خارج تخصصك (لغة، فن، رياضة) لتوسيع دائرتك. اسلك شارعاً جديداً كل أسبوع.\n\n" +
        "## متى تطلب الدعم\n\nاستمرار المزاج المنخفض بعد الأسبوع العاشر ليس حنيناً عادياً للوطن. إرشاد الجامعة عادة مجاني وسري — استخدمه.\n";

    public const string VisaTipsEn =
        "# Student Visa Application Tips — Avoiding Common Rejections\n\n" +
        "Consular officers reject visas for surprisingly consistent reasons. Knowing them ahead of time eliminates most of the risk.\n\n" +
        "## Demonstrate intent to return\n\nMany student visas test for \"non-immigrant intent\" — evidence that you plan to leave the country after your studies. Family ties, property, a job offer at home or letters from a future employer all help. The absence of any tie raises questions.\n\n" +
        "## Show clean, organised finances\n\nLarge transfers in the days before your application look suspicious. If a sponsor is funding you, the sponsorship letter, the sponsor's bank statements and a clear relationship document should be in the file. The 28-day rule (funds in the named account for 28 consecutive days) catches many applicants.\n\n" +
        "## Match your stated plan to your CV\n\nIf your CV is in marketing but you say you will study computer science, expect a tough interview. Either prepare to explain the pivot clearly or rethink the programme.\n\n" +
        "## Bring originals\n\nEven when you uploaded scans, bring originals to the interview. Officers often ask. Showing up without an original is a soft signal of disorganisation.\n\n" +
        "## Be brief and direct\n\nLong, defensive answers signal nervousness. Short, clear answers signal confidence. Practise saying your plan out loud in twenty seconds.\n";

    public const string VisaTipsAr =
        "# نصائح طلب تأشيرة الدراسة — تجنّب أسباب الرفض الشائعة\n\n" +
        "يرفض ضباط القنصلية التأشيرات لأسباب متسقة بشكل مفاجئ. معرفتها مسبقاً تزيل معظم المخاطر.\n\n" +
        "## أثبت نية العودة\n\nكثير من تأشيرات الطلاب تختبر \"نية غير مهاجرة\" — دليل أنك تخطط لمغادرة البلد بعد الدراسة. الروابط العائلية، الممتلكات، عرض عمل في الوطن، أو خطابات من صاحب عمل مستقبلي كلها تساعد. غياب أي رابط يثير أسئلة.\n\n" +
        "## أظهر مالية نظيفة ومنظمة\n\nالتحويلات الكبيرة في الأيام قبل الطلب تبدو مريبة. إذا كان كفيل يموّلك، يجب أن تكون خطاب الكفالة وكشوف حساب الكفيل ووثيقة العلاقة الواضحة في الملف. قاعدة 28 يوماً (الأموال في الحساب المسجل لـ28 يوماً متتالية) تُفاجئ كثيرين.\n\n" +
        "## طابق خطتك المعلنة مع سيرتك\n\nإذا كانت سيرتك في التسويق لكنك تقول إنك ستدرس علوم الحاسوب، توقع مقابلة صعبة. إما استعد لشرح التحول بوضوح أو أعد التفكير في البرنامج.\n\n" +
        "## أحضر الأصول\n\nحتى لو رفعت نسخاً ممسوحة، أحضر الأصول إلى المقابلة. يطلبها الضباط كثيراً. الحضور بدون أصل إشارة خفيفة لعدم التنظيم.\n\n" +
        "## كن مختصراً ومباشراً\n\nالإجابات الطويلة الدفاعية تشير إلى توتر. الإجابات القصيرة الواضحة تشير إلى ثقة. درب نفسك على قول خطتك بصوت مرتفع في عشرين ثانية.\n";

    // ─────────────────────────────────────────────────────────────────────
    //                              VIDEOS
    // ─────────────────────────────────────────────────────────────────────

    public const string WebinarInterviewEn =
        "# Webinar: Interview Preparation Masterclass\n\n" +
        "A 60-minute recorded session walking through three real interview scenarios — a Master's scholarship panel, a doctoral fellowship board and a country-specific government interview. The session is led by a former selection-committee member and recorded with two student volunteers.\n\n" +
        "## What you will learn\n\nHow to read the format of an interview from the way the invitation is worded. The two warm-up questions that almost always come first. The three categories of follow-up questions to prepare for. How to ask intelligent questions at the end.\n\n" +
        "## Format\n\nThe video alternates between commentary, live mock interviews and on-screen notes summarising the key takeaways. You can skip to a specific scenario from the chapter index.\n\n" +
        "## Recommended order\n\nWatch the introduction first, then the scenario closest to your own situation. The other two are useful but optional.\n";

    public const string WebinarInterviewAr =
        "# ندوة: الدرس المتقدم في التحضير للمقابلة\n\n" +
        "جلسة مسجلة لمدة 60 دقيقة تستعرض ثلاثة سيناريوهات مقابلات حقيقية — لجنة منحة ماجستير، ومجلس زمالة دكتوراه، ومقابلة حكومية لدولة محددة. يقود الجلسة عضو سابق في لجنة اختيار، وتُسجَّل مع طالبَين متطوعَين.\n\n" +
        "## ما ستتعلمه\n\nكيف تقرأ شكل المقابلة من طريقة صياغة الدعوة. السؤالان التمهيديان اللذان يأتيان أولاً تقريباً دائماً. الفئات الثلاث لأسئلة المتابعة التي تستعد لها. كيف تطرح أسئلة ذكية في النهاية.\n\n" +
        "## الشكل\n\nيتناوب الفيديو بين التعليق والمقابلات التجريبية المباشرة والملاحظات على الشاشة التي تلخص الخلاصات الرئيسية. يمكنك القفز إلى سيناريو محدد من فهرس الفصول.\n\n" +
        "## الترتيب الموصى به\n\nشاهد المقدمة أولاً، ثم السيناريو الأقرب إلى وضعك. الآخران مفيدان لكنهما اختياريان.\n";

    public const string WebinarSopEn =
        "# Webinar: Crafting a Standout Statement of Purpose\n\n" +
        "Forty-five minutes of walking through three real personal statements with full rewrites. Each one starts as a draft a student submitted, and the session shows how to rebuild it paragraph by paragraph.\n\n" +
        "## Statements covered\n\nA Master's application that buried the lead under a long childhood story; a doctoral statement that listed accomplishments but never explained the research question; an undergraduate statement that read more like a CV than an essay.\n\n" +
        "## What you will take away\n\nA repeatable opening structure, three techniques for connecting paragraphs, and a checklist of red-flag phrases reviewers spot in seconds.\n\n" +
        "## Audience\n\nUseful for first-time applicants and for anyone whose draft has been read by a friend who said \"it's fine\" — which usually means it is not.\n";

    public const string WebinarSopAr =
        "# ندوة: كتابة خطاب غرض متميز\n\n" +
        "خمس وأربعون دقيقة من استعراض ثلاث بيانات شخصية حقيقية مع إعادة كتابة كاملة. كل واحدة تبدأ كمسودة قدّمها طالب، وتُظهر الجلسة كيف يُعاد بناؤها فقرة بفقرة.\n\n" +
        "## البيانات المغطاة\n\nطلب ماجستير دفن الرسالة تحت قصة طفولة طويلة؛ بيان دكتوراه عدّد الإنجازات لكنه لم يشرح سؤال البحث؛ بيان جامعي قرأ كسيرة ذاتية أكثر منه مقالاً.\n\n" +
        "## ما ستأخذه\n\nهيكل افتتاحي قابل للتكرار، ثلاث تقنيات لربط الفقرات، وقائمة عبارات تنبه يلاحظها المراجعون في ثوانٍ.\n\n" +
        "## الجمهور\n\nمفيدة للمتقدمين لأول مرة ولكل من قرأ مسودته صديق وقال \"إنها جيدة\" — وهو ما يعني عادة أنها ليست كذلك.\n";

    public const string WorkshopIeltsEn =
        "# Workshop: IELTS Speaking Practice Live\n\n" +
        "A live workshop recorded with three candidates over ninety minutes. Each candidate runs through all three IELTS Speaking parts, and the examiner-trained instructor offers real-time feedback after every answer.\n\n" +
        "## Why watch\n\nSpeaking practice is the section students under-prepare for, because it is hard to self-assess. Watching a real candidate get specific feedback on grammar range, fluency, pronunciation and coherence is more useful than reading any general guide.\n\n" +
        "## Chapter index\n\nThe video is indexed by section and by candidate, so you can jump directly to the patterns most relevant to your weak areas.\n\n" +
        "## After watching\n\nThe workshop ends with a five-minute self-evaluation prompt — answer the questions yourself and compare your responses to the ones the candidates gave.\n";

    public const string WorkshopIeltsAr =
        "# ورشة عمل: تدريب مباشر على المحادثة في الآيلتس\n\n" +
        "ورشة عمل مباشرة مسجلة مع ثلاثة مرشحين خلال تسعين دقيقة. كل مرشح يجتاز الأجزاء الثلاثة للمحادثة في الآيلتس، ويقدم المدرب المدرّب كممتحن ملاحظات فورية بعد كل إجابة.\n\n" +
        "## لماذا تشاهد\n\nتدريب المحادثة هو القسم الذي يقلل الطلاب التحضير له، لأنه يصعب تقييمه ذاتياً. مشاهدة مرشح حقيقي يتلقى ملاحظات محددة عن نطاق القواعد والطلاقة والنطق والتماسك أنفع من قراءة أي دليل عام.\n\n" +
        "## فهرس الفصول\n\nالفيديو مفهرس بالقسم والمرشح، فيمكنك القفز مباشرة إلى الأنماط الأكثر صلة بمناطق ضعفك.\n\n" +
        "## بعد المشاهدة\n\nتنتهي الورشة بسؤال تقييم ذاتي مدته خمس دقائق — أجب عن الأسئلة بنفسك وقارن إجاباتك بإجابات المرشحين.\n";

    public const string PanelGermanyEn =
        "# Panel: Life in Germany as an International Student\n\n" +
        "A 75-minute recorded panel with three international students in Berlin, Munich and Hamburg, covering their first year in Germany.\n\n" +
        "## Topics covered\n\nFinding accommodation in three different rental markets; opening a bank account that actually works for international transfers; the difference between public and private health insurance; learning enough German to handle bureaucracy; making friends outside your degree programme.\n\n" +
        "## Why three cities\n\nGermany's regions are surprisingly different. Berlin's renter culture, Munich's tight housing market and Hamburg's quieter pace each shape student life in their own way. The panel highlights the contrasts.\n\n" +
        "## Worth watching for\n\nThe Q&A in the final twenty minutes, where the panellists answer audience questions about Anmeldung (city registration), part-time work, and the academic culture differences between their home universities and German ones.\n";

    public const string PanelGermanyAr =
        "# حلقة نقاش: الحياة في ألمانيا كطالب دولي\n\n" +
        "حلقة نقاش مسجلة لمدة 75 دقيقة مع ثلاثة طلاب دوليين في برلين وميونيخ وهامبورغ، تغطي سنتهم الأولى في ألمانيا.\n\n" +
        "## الموضوعات المغطاة\n\nإيجاد سكن في ثلاث أسواق إيجار مختلفة؛ فتح حساب بنكي يعمل فعلاً للتحويلات الدولية؛ الفرق بين التأمين الصحي العام والخاص؛ تعلم ما يكفي من الألمانية للتعامل مع البيروقراطية؛ تكوين صداقات خارج برنامج دراستك.\n\n" +
        "## لماذا ثلاث مدن\n\nمناطق ألمانيا مختلفة بشكل مفاجئ. ثقافة الإيجار في برلين، وسوق السكن المتوتر في ميونيخ، ووتيرة هامبورغ الأهدأ — كل منها يشكّل حياة الطالب بطريقتها. تُبرز الحلقة هذه التباينات.\n\n" +
        "## يستحق المشاهدة لأجل\n\nالأسئلة والأجوبة في العشرين دقيقة الأخيرة، حيث يجيب المتحدثون عن أسئلة الجمهور حول التسجيل في المدينة، والعمل الجزئي، واختلافات الثقافة الأكاديمية بين جامعاتهم الأصلية والألمانية.\n";

    public const string WebinarFundingEn =
        "# Webinar: Funding a Master's Degree With Multiple Sources\n\n" +
        "A practical 50-minute walkthrough of how three former students assembled funding for their Master's degrees by stacking five or more sources each.\n\n" +
        "## What is covered\n\nHow to negotiate a tuition waiver after acceptance; the email template that opens a conversation with the department graduate coordinator; how to identify and apply to local foundations that fund small grants; the work-study programmes that align with student visa rules; and what to do when one source falls through halfway through the year.\n\n" +
        "## Realistic numbers\n\nThe webinar includes a spreadsheet template you can copy. Real numbers, no marketing.\n\n" +
        "## For whom\n\nStudents who already have a partial scholarship and need to close a funding gap of 30 to 60 per cent.\n";

    public const string WebinarFundingAr =
        "# ندوة: تمويل الماجستير من مصادر متعددة\n\n" +
        "شرح عملي لمدة 50 دقيقة لكيف جمع ثلاثة طلاب سابقين تمويل ماجستيرهم بتراكم خمسة مصادر أو أكثر لكل واحد.\n\n" +
        "## ما يُغطى\n\nكيف تتفاوض على إعفاء رسوم بعد القبول؛ قالب البريد الإلكتروني الذي يفتح محادثة مع منسق الدراسات العليا في القسم؛ كيف تحدد المؤسسات المحلية التي تموّل منحاً صغيرة وتتقدم لها؛ برامج العمل-الدراسة التي تتماشى مع قواعد تأشيرة الطالب؛ وماذا تفعل عندما يفشل أحد المصادر في منتصف العام.\n\n" +
        "## أرقام واقعية\n\nتتضمن الندوة قالب جدول بيانات يمكنك نسخه. أرقام حقيقية، لا تسويق.\n\n" +
        "## لمن\n\nالطلاب الذين لديهم بالفعل منحة جزئية ويحتاجون إغلاق فجوة تمويلية بين 30 و60 بالمئة.\n";

    public const string WalkthroughPsEn =
        "# Walkthrough: Writing the Personal Statement Live\n\n" +
        "An hour-long screen-share recording of a personal statement being drafted from outline to final paragraph. The author thinks out loud — you see the false starts, the rewrites, and the trade-offs at every paragraph.\n\n" +
        "## Why this format\n\nMost guides show finished essays and tell you what good looks like. This walkthrough shows the process — including the long pauses, the sentences cut after rereading, and the decision to scrap an opening that did not earn its place.\n\n" +
        "## What you'll see\n\nThe outline-first method (twenty minutes before writing); the use of a one-sentence thesis to keep paragraphs on track; the technique of writing the closing paragraph second, not last; and the read-aloud pass that catches the awkward sentences.\n\n" +
        "## Take-home\n\nA template document mirroring the structure used in the walkthrough, ready for you to adapt.\n";

    public const string WalkthroughPsAr =
        "# شرح تفاعلي: كتابة البيان الشخصي مباشرة\n\n" +
        "تسجيل مشاركة شاشة لمدة ساعة لكتابة بيان شخصي من المخطط إلى الفقرة النهائية. يفكر الكاتب بصوت مرتفع — ترى البدايات الخاطئة، وإعادة الكتابة، والمفاضلات في كل فقرة.\n\n" +
        "## لماذا هذا الشكل\n\nمعظم الأدلة تعرض مقالات مكتملة وتخبرك كيف يبدو الجيد. هذا الشرح يعرض العملية — بما فيها الوقفات الطويلة، والجمل المحذوفة بعد القراءة، وقرار التخلي عن افتتاحية لم تستحق مكانها.\n\n" +
        "## ما ستراه\n\nطريقة المخطط أولاً (عشرون دقيقة قبل الكتابة)؛ استخدام أطروحة من جملة واحدة لإبقاء الفقرات على المسار؛ تقنية كتابة الفقرة الختامية ثانية لا أخيرة؛ ومرحلة القراءة بصوت مرتفع التي تلتقط الجمل المحرجة.\n\n" +
        "## ما تأخذه\n\nمستند قالب يحاكي الهيكل المستخدم في الشرح، جاهز لتكييفه.\n";

    // ─────────────────────────────────────────────────────────────────────
    //                              CHECKLISTS
    // ─────────────────────────────────────────────────────────────────────

    public const string PreSubmitChecklistEn =
        "# Pre-Submission Document Checklist\n\n" +
        "Run through this two days before you submit. If anything is unchecked, you have time to fix it. If you discover gaps on submission day, you do not.\n\n" +
        "## Academic\n\n- [ ] Official transcript (current and complete)\n- [ ] Degree certificate or proof of expected graduation\n- [ ] Recommendation letters (at least two, on letterhead, signed)\n- [ ] Proof of language proficiency (IELTS / TOEFL valid for at least 18 more months)\n\n" +
        "## Essay materials\n\n- [ ] Personal statement (final version, within word limit, file named correctly)\n- [ ] CV or resume (one or two pages, current, ATS-friendly)\n- [ ] Research proposal if required (page count and font size match the brief)\n\n" +
        "## Identity and travel\n\n- [ ] Passport copy (valid at least six months past expected travel)\n- [ ] National ID or birth certificate copy\n- [ ] Recent passport-style photograph if requested\n\n" +
        "## Financial\n\n- [ ] Bank statements covering the requested period\n- [ ] Sponsorship letter and sponsor ID copy if applicable\n- [ ] Proof of any other funding awards already received\n\n" +
        "## Administrative\n\n- [ ] All forms signed and dated\n- [ ] File names follow the scholarship's naming convention\n- [ ] File size under any stated limit\n- [ ] Submission confirmation page screenshot saved\n";

    public const string PreSubmitChecklistAr =
        "# قائمة التحقق من المستندات قبل التقديم\n\n" +
        "راجع هذه القائمة قبل التقديم بيومين. إذا كان شيء غير مكتمل، لديك وقت لإصلاحه. إذا اكتشفت ثغرات يوم التقديم، فلا وقت لديك.\n\n" +
        "## أكاديمي\n\n- [ ] كشف درجات رسمي (حالي وكامل)\n- [ ] شهادة التخرج أو إثبات التخرج المتوقع\n- [ ] خطابات التوصية (اثنان على الأقل، على ورق رسمي، موقعة)\n- [ ] إثبات إتقان اللغة (IELTS / TOEFL ساري لمدة 18 شهراً إضافياً على الأقل)\n\n" +
        "## مواد المقال\n\n- [ ] البيان الشخصي (النسخة النهائية، ضمن حد الكلمات، اسم الملف صحيح)\n- [ ] السيرة الذاتية (صفحة أو صفحتان، حالية، متوافقة مع أنظمة الفرز)\n- [ ] اقتراح بحثي إذا طُلب (عدد الصفحات وحجم الخط مطابقان للتعليمات)\n\n" +
        "## الهوية والسفر\n\n- [ ] نسخة جواز السفر (سارية ستة أشهر على الأقل بعد السفر المتوقع)\n- [ ] نسخة الهوية الوطنية أو شهادة الميلاد\n- [ ] صورة شخصية حديثة بنمط جواز السفر إذا طُلبت\n\n" +
        "## مالي\n\n- [ ] كشوف حساب بنكية تغطي الفترة المطلوبة\n- [ ] خطاب كفالة ونسخة هوية الكفيل إذا انطبق\n- [ ] إثبات أي تمويلات أخرى مستلمة بالفعل\n\n" +
        "## إداري\n\n- [ ] جميع النماذج موقعة ومؤرخة\n- [ ] أسماء الملفات تتبع تسمية المنحة\n- [ ] حجم الملف ضمن أي حد معلن\n- [ ] لقطة شاشة لصفحة تأكيد التقديم محفوظة\n";

    public const string VisaInterviewChecklistEn =
        "# Visa Interview Day Checklist\n\n" +
        "Print this. Bring the printed version with you and tick items as you finish them.\n\n" +
        "## The night before\n\n- [ ] Confirmed appointment time and location\n- [ ] Printed appointment confirmation\n- [ ] Outfit ready (business casual, neat)\n- [ ] Alarm set with a 60-minute buffer\n- [ ] Phone fully charged, charger packed\n\n" +
        "## Documents — originals AND copies\n\n- [ ] Valid passport plus all old passports\n- [ ] Visa application form (DS-160 / similar) printed\n- [ ] Visa fee payment receipt\n- [ ] Acceptance letter from the institution\n- [ ] Tuition payment receipts (any deposit paid)\n- [ ] Financial evidence (bank statements, scholarship award letters, sponsorship documents)\n- [ ] Academic transcripts and certificates\n- [ ] Language test results\n- [ ] Two passport-style photographs\n\n" +
        "## At the consulate\n\n- [ ] Arrived 30 minutes early\n- [ ] Phone on silent, ideally checked at the security desk\n- [ ] Documents organised in the order an officer would ask for them\n- [ ] Stayed off the phone and chatty topics with strangers in the waiting area\n\n" +
        "## After the interview\n\n- [ ] Made a note of what was asked while it is fresh\n- [ ] Followed any instructions for passport return delivery\n";

    public const string VisaInterviewChecklistAr =
        "# قائمة التحقق ليوم مقابلة التأشيرة\n\n" +
        "اطبع هذه. أحضر النسخة المطبوعة معك واشطب البنود حين تنتهي منها.\n\n" +
        "## الليلة السابقة\n\n- [ ] تأكيد موعد ومكان المقابلة\n- [ ] طباعة تأكيد الموعد\n- [ ] الزي جاهز (عمل غير رسمي، أنيق)\n- [ ] ضبط المنبه مع هامش 60 دقيقة\n- [ ] الهاتف مشحون بالكامل، الشاحن معبأ\n\n" +
        "## المستندات — أصول ونسخ\n\n- [ ] جواز سفر ساري بالإضافة إلى كل الجوازات القديمة\n- [ ] طلب التأشيرة (DS-160 / مماثل) مطبوع\n- [ ] إيصال دفع رسوم التأشيرة\n- [ ] خطاب القبول من المؤسسة\n- [ ] إيصالات دفع الرسوم (أي وديعة دُفعت)\n- [ ] الأدلة المالية (كشوف بنكية، خطابات منح، وثائق كفالة)\n- [ ] كشوف الدرجات والشهادات الأكاديمية\n- [ ] نتائج اختبار اللغة\n- [ ] صورتان بنمط جواز السفر\n\n" +
        "## في القنصلية\n\n- [ ] الوصول قبل 30 دقيقة\n- [ ] الهاتف على وضع صامت، ويُفضل تركه في مكتب الأمن\n- [ ] المستندات منظمة بترتيب يطلبها به الضابط\n- [ ] الابتعاد عن الهاتف والحديث الكثير مع الغرباء في صالة الانتظار\n\n" +
        "## بعد المقابلة\n\n- [ ] تدوين ما سُئل عنه بينما الذاكرة طازجة\n- [ ] اتباع أي تعليمات لتسليم جواز السفر بعد الإصدار\n";

    public const string PreDepartureChecklistEn =
        "# Pre-Departure Packing & Paperwork Checklist\n\n" +
        "Work through this two weeks before you fly. Some tasks need lead time you cannot recover.\n\n" +
        "## Two weeks out\n\n- [ ] Travel insurance purchased to cover the gap before destination insurance starts\n- [ ] Notified home bank of foreign use\n- [ ] Confirmed housing arrival window with landlord / university\n- [ ] Booked airport transfer or arrival pickup\n- [ ] Printed map of route from airport to housing\n\n" +
        "## One week out\n\n- [ ] Prescription medication refilled (60 days minimum) + doctor's note\n- [ ] Vaccination record copied to phone and paper folder\n- [ ] Cash in destination currency for the first two days\n- [ ] Power adapters for the destination's plug type\n\n" +
        "## Documents bag (carry-on, not checked)\n\n- [ ] Passport with visa\n- [ ] Acceptance letter and housing contract\n- [ ] Insurance policy documents\n- [ ] Prescription and medical history summary\n- [ ] Copies of all of the above stored in your email\n\n" +
        "## Packing\n\n- [ ] Three weeks of clothing, not the full year\n- [ ] Universal adapter and a power strip\n- [ ] Comfortable walking shoes (already broken in)\n- [ ] One favourite item from home that does not weigh much (a photo, a small ornament)\n\n" +
        "## Goodbyes\n\n- [ ] Family farewell planned (small, not a late party)\n- [ ] Friends informed of move\n- [ ] Forwarding plan for any mail at home address\n";

    public const string PreDepartureChecklistAr =
        "# قائمة التحقق من التعبئة والأوراق قبل السفر\n\n" +
        "أنجز هذه القائمة قبل السفر بأسبوعين. بعض المهام تتطلب وقتاً لا يمكن استرداده.\n\n" +
        "## قبل أسبوعين\n\n- [ ] شراء تأمين سفر يغطي الفجوة قبل بدء تأمين الوجهة\n- [ ] إبلاغ بنك الوطن بالاستخدام الأجنبي\n- [ ] تأكيد نافذة الوصول للسكن مع المالك / الجامعة\n- [ ] حجز نقل من المطار أو استلام عند الوصول\n- [ ] طباعة خريطة الطريق من المطار إلى السكن\n\n" +
        "## قبل أسبوع\n\n- [ ] تعبئة وصفة الأدوية (60 يوماً على الأقل) + ملاحظة من الطبيب\n- [ ] نسخ سجل التطعيمات إلى الهاتف ومجلد ورقي\n- [ ] نقد بعملة الوجهة لليومين الأولين\n- [ ] محولات كهرباء لنوع قابس الوجهة\n\n" +
        "## حقيبة المستندات (حقيبة يد، ليست مسجلة)\n\n- [ ] جواز السفر مع التأشيرة\n- [ ] خطاب القبول وعقد السكن\n- [ ] وثائق بوليصة التأمين\n- [ ] الوصفة الطبية وملخص التاريخ الطبي\n- [ ] نسخ من كل ما سبق مخزنة في بريدك الإلكتروني\n\n" +
        "## التعبئة\n\n- [ ] ملابس لثلاثة أسابيع، لا للسنة كاملة\n- [ ] محول عالمي ومشترك كهرباء\n- [ ] حذاء مشي مريح (مكسور بالفعل)\n- [ ] غرض مفضل واحد من الوطن لا يزن كثيراً (صورة، زينة صغيرة)\n\n" +
        "## الوداع\n\n- [ ] وداع عائلي مخطط (صغير، ليس حفلة متأخرة)\n- [ ] إبلاغ الأصدقاء بالانتقال\n- [ ] خطة تحويل أي بريد على عنوان الوطن\n";

    public const string FirstWeekChecklistEn =
        "# First-Week-Abroad Settling-In Checklist\n\n" +
        "The first week unlocks everything else. These tasks are the difference between a smooth term and three months of friction.\n\n" +
        "## Day one\n\n- [ ] Confirmed safe arrival at housing\n- [ ] Tested water, electricity and Wi-Fi work\n- [ ] Located the nearest grocery store and pharmacy\n- [ ] Slept early — jet lag adds up\n\n" +
        "## Day two and three\n\n- [ ] Bought a local SIM card or activated an eSIM\n- [ ] Set up a local bank account or activated an international account\n- [ ] Picked up a transit pass (monthly or student card)\n- [ ] Found a quiet, reliable place to study near home\n\n" +
        "## Day four to seven\n\n- [ ] Registered with local authorities if your visa requires it (within 14 days is typical)\n- [ ] Picked up your student ID at the university\n- [ ] Met an academic advisor or programme coordinator\n- [ ] Joined at least one new circle outside your degree (language exchange, sport, religious community)\n- [ ] Sent one update home — your family is anxious\n\n" +
        "## End of week one\n\n- [ ] One repeatable weekly routine in place (a class, a sport, a study group)\n- [ ] Found a doctor or clinic you would call in an emergency\n";

    public const string FirstWeekChecklistAr =
        "# قائمة التحقق للاستقرار في الأسبوع الأول بالخارج\n\n" +
        "الأسبوع الأول يفتح كل شيء آخر. هذه المهام هي الفرق بين فصل دراسي سلس وثلاثة أشهر من الاحتكاك.\n\n" +
        "## اليوم الأول\n\n- [ ] تأكيد الوصول الآمن إلى السكن\n- [ ] التحقق من عمل الماء والكهرباء والواي فاي\n- [ ] تحديد أقرب بقالة وصيدلية\n- [ ] النوم مبكراً — يتراكم الإرهاق\n\n" +
        "## اليومان الثاني والثالث\n\n- [ ] شراء شريحة هاتف محلية أو تفعيل eSIM\n- [ ] إنشاء حساب بنكي محلي أو تفعيل حساب دولي\n- [ ] استخراج بطاقة مواصلات (شهرية أو طالب)\n- [ ] إيجاد مكان هادئ وموثوق للدراسة قرب السكن\n\n" +
        "## اليوم الرابع إلى السابع\n\n- [ ] التسجيل لدى السلطات المحلية إذا اشترطت التأشيرة (عادة خلال 14 يوماً)\n- [ ] استلام البطاقة الجامعية\n- [ ] لقاء مرشد أكاديمي أو منسق البرنامج\n- [ ] الانضمام إلى دائرة جديدة واحدة على الأقل خارج تخصصك (تبادل لغوي، رياضة، مجتمع ديني)\n- [ ] إرسال تحديث واحد للأهل — الأسرة قلقة\n\n" +
        "## نهاية الأسبوع الأول\n\n- [ ] روتين أسبوعي قابل للتكرار في مكانه (صف، رياضة، مجموعة دراسة)\n- [ ] إيجاد طبيب أو عيادة تتصل بها في حالة الطوارئ\n";

    public const string LanguageChecklistEn =
        "# Language-Test Preparation Weekly Checklist\n\n" +
        "Eight weeks is the right preparation window for most candidates targeting IELTS 7 or TOEFL 100. Each week below assumes about ten hours of study.\n\n" +
        "## Each week\n\n- [ ] Two full timed sections (different sections each time)\n- [ ] One vocabulary set of 25 high-frequency academic words\n- [ ] One speaking practice with a partner or recorded self-review\n- [ ] One writing task graded against the official rubric\n\n" +
        "## Week 1\n\n- [ ] Baseline mock test complete\n- [ ] Weakest section identified\n- [ ] Study plan written down\n\n" +
        "## Weeks 2 to 4\n\n- [ ] Targeted drills on weakest section daily\n- [ ] Daily reading of a long article in English\n- [ ] First full timed mock score logged\n\n" +
        "## Weeks 5 to 7\n\n- [ ] Two full timed mocks per week\n- [ ] Writing tasks marked against the rubric, common errors logged\n- [ ] Speaking responses recorded and self-reviewed\n\n" +
        "## Final week\n\n- [ ] No new content\n- [ ] One light mock to keep timing reflexes sharp\n- [ ] Test centre route confirmed\n- [ ] Identity documents, water and snacks packed for the test day\n";

    public const string LanguageChecklistAr =
        "# قائمة التحقق الأسبوعية للتحضير لاختبار اللغة\n\n" +
        "ثمانية أسابيع هي نافذة التحضير المناسبة لمعظم المرشحين المستهدفين IELTS 7 أو TOEFL 100. كل أسبوع أدناه يفترض حوالي عشر ساعات من الدراسة.\n\n" +
        "## كل أسبوع\n\n- [ ] قسمان كاملان بتوقيت (أقسام مختلفة في كل مرة)\n- [ ] مجموعة مفردات من 25 كلمة أكاديمية شائعة\n- [ ] تدريب محادثة مع شريك أو مراجعة ذاتية مسجلة\n- [ ] مهمة كتابة مقيمة وفق المعيار الرسمي\n\n" +
        "## الأسبوع الأول\n\n- [ ] إكمال اختبار تجريبي قاعدي\n- [ ] تحديد القسم الأضعف\n- [ ] كتابة خطة الدراسة\n\n" +
        "## الأسابيع 2 إلى 4\n\n- [ ] تدريبات مستهدفة على القسم الأضعف يومياً\n- [ ] قراءة يومية لمقال طويل بالإنجليزية\n- [ ] تسجيل أول درجة لاختبار تجريبي كامل بتوقيت\n\n" +
        "## الأسابيع 5 إلى 7\n\n- [ ] اختباران تجريبيان كاملان بتوقيت أسبوعياً\n- [ ] تصحيح مهام الكتابة وفق المعيار، وتسجيل الأخطاء الشائعة\n- [ ] تسجيل ردود المحادثة ومراجعتها ذاتياً\n\n" +
        "## الأسبوع الأخير\n\n- [ ] لا محتوى جديد\n- [ ] اختبار تجريبي خفيف للحفاظ على ردود فعل التوقيت\n- [ ] تأكيد طريق مركز الاختبار\n- [ ] تجهيز وثائق الهوية والماء والوجبات الخفيفة ليوم الاختبار\n";
}
