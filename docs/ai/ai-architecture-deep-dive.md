# ScholarPath — الغوص العميق في معمارية الذكاء الاصطناعي

> **الهدف من الملف:**  
> شرح تفصيلي وشامل لكل ما يحدث في الجزء الخاص بالذكاء الاصطناعي في ScholarPath —
> المفاهيم النظرية، وكيفية تطبيقها في الكود، وكل خطوة بالتسلسل الكامل.
> مرجع للمناقشة الأكاديمية وفهم القرارات التصميمية.

---

## فهرس المحتويات

1. [المفاهيم الأساسية — ما يجب فهمه قبل كل شيء](#١-المفاهيم-الأساسية)
2. [Standard RAG مقابل Agentic RAG](#٢-standard-rag-مقابل-agentic-rag)
3. [معمارية الـ AI في ScholarPath — الصورة الكاملة](#٣-معمارية-الـ-ai-في-scholarpath)
4. [قاعدة المعرفة — بناؤها وصيانتها](#٤-قاعدة-المعرفة)
5. [Chatbot — الـ RAG Pipeline الكاملة خطوة بخطوة](#٥-chatbot-pipeline)
6. [نظام الترشيحات — Rule-Based Scoring](#٦-نظام-الترشيحات)
7. [فحص الأهلية — Eligibility Check](#٧-فحص-الأهلية)
8. [Fallback Chain — سلسلة الاحتياط](#٨-fallback-chain)
9. [التكاليف والـ Token Accounting](#٩-التكاليف)
10. [Fine-Tuning — الخطوة القادمة](#١٠-fine-tuning)
11. [لماذا لسنا Agentic RAG؟](#١١-لماذا-لسنا-agentic-rag)
12. [ملخص المكونات والملفات](#١٢-ملخص-المكونات)

---

## ١. المفاهيم الأساسية

### ١.١ ما هو الـ Embedding؟

الـ **Embedding** هو تحويل أي نص (جملة، فقرة، مقال كامل) إلى **متجه أرقام** (Array of floats).

```
نص: "منحة دراسية للطلاب المصريين في فرنسا"
            ↓  Embedding Model
متجه: [0.12, -0.45, 0.88, 0.03, -0.71, ..., 0.29]  (1536 رقم)
```

**لماذا مفيد؟**  
- النصوص المتشابهة في المعنى تنتج متجهات قريبة من بعض في الفضاء الرياضي.
- جملة "How to apply for scholarships in Germany" وجملة "طريقة التقديم للمنح في ألمانيا" ستعطيان متجهين **قريبين جداً** رغم اختلاف اللغة.
- هذا يجعل البحث **دلالياً (Semantic)** لا مجرد مطابقة كلمات.

**في ScholarPath:** يستخدم نموذج `text-embedding-3-small` من Azure OpenAI، يُنتج متجهات بـ **1536 بُعد** لكل نص.

---

### ١.٢ ما هو الـ Cosine Similarity؟

بعد تحويل النصوص لمتجهات، نقيس مدى قربها من بعض بحساب **الزاوية بينهما**.

```
                   dot(A, B)
cos(θ) = ─────────────────────────────
          ||A|| × ||B||
```

- النتيجة بين **-1 و +1** (عملياً 0 إلى 1 للنصوص).
- **1.0** = النصان متطابقان في المعنى.
- **0.0** = لا علاقة بينهما.
- **0.7+** = تشابه قوي في السياق المنصة دي.

**الكود في ScholarPath** (`VectorMath.cs`):
```csharp
public static double CosineSimilarity(float[] a, float[] b)
{
    double dot = 0, normA = 0, normB = 0;
    for (var i = 0; i < a.Length; i++)
    {
        dot   += (double)a[i] * b[i];
        normA += (double)a[i] * a[i];
        normB += (double)b[i] * b[i];
    }
    var denom = Math.Sqrt(normA) * Math.Sqrt(normB);
    return denom < 1e-9 ? 0 : dot / denom;
}
```

---

### ١.٣ ما هو الـ LLM (Large Language Model)؟

نموذج لغوي ضخم (مثل GPT-4o-mini) يستطيع:
- فهم النص وتوليده
- الإجابة على الأسئلة
- التلخيص والترجمة
- اتباع تعليمات محددة (System Prompt)

**المشكلة الجوهرية للـ LLM بدون RAG:**
- يعرف فقط ما تدرّب عليه (حتى تاريخ قطع المعرفة).
- لا يعرف شيئاً عن منح ScholarPath الحالية.
- قد يُلفق (hallucinate) معلومات غير صحيحة.

---

### ١.٤ ما هو الـ RAG (Retrieval-Augmented Generation)؟

**RAG** هو تقنية تُضاف لـ LLM لحل مشكلة المعرفة المحدودة:

```
سؤال المستخدم
      ↓
[البحث في قاعدة المعرفة] ← يُجلب المحتوى الأنسب
      ↓
[حقن المحتوى في الـ Prompt] ← الـ LLM يرى السياق الحقيقي
      ↓
[توليد إجابة مبنية على البيانات الحقيقية]
```

**الفرق البسيط:**
- بدون RAG: GPT يجيب من ذاكرته العامة
- مع RAG: GPT يجيب من بيانات ScholarPath الحقيقية

---

## ٢. Standard RAG مقابل Agentic RAG

### ٢.١ Standard RAG — Pipeline خطي

```
المستخدم → Embed(سؤال) → بحث في KB → أفضل K نتيجة → GPT + Context → إجابة
```

**المشكلة:** لا يوجد evaluation loop — النظام لا يتوقف ليتحقق: "هل النتائج كافية؟"

**أمثلة على فشله:**
- سؤال غامض ("كيف أتقدم؟") — لا يطلب توضيح
- سؤال يحتاج مصادر متعددة — يجيب بمصدر واحد فقط
- نتائج الـ retrieval ضعيفة — يُجيب بثقة رغم ذلك

---

### ٢.٢ Agentic RAG — Control Loop

بدلاً من pipeline خطي، يضيف **وكيل (Agent)** يتخذ قرارات:

```
المستخدم
    ↓
  [Agent]
    ├── هل السؤال واضح؟ → لا → اطلب توضيح
    ├── أي مصدر معرفة مناسب؟ → اختر المناسب
    ↓
  Retrieve
    ↓
  [Agent تقييم]
    ├── هل النتائج كافية؟ → لا → أعد الصياغة وابحث مرة أخرى
    ├── هل يحتاج مصادر إضافية؟ → نعم → ابحث في مصدر آخر
    ↓
  Generate
    ↓
  إجابة
```

**القدرات الثلاث الأساسية للـ Agentic RAG:**
1. **Tool Use & Routing** — اختيار مصدر المعرفة المناسب
2. **Query Refinement** — إعادة صياغة السؤال إن كانت النتائج رديئة
3. **Self-Evaluation** — تقييم ما إذا كانت النتائج تجيب السؤال فعلاً

**تكاليف Agentic RAG:**
- الـ Latency: 10+ ثواني مقابل 1-2 ثانية
- التكلفة: 3-10x أغلى (LLM calls إضافية للتقييم)
- التعقيد: debugging صعب (سلوك غير حتمي)
- **The Evaluator Paradox:** نفس الـ LLM الذي يُجيب هو من يُقيّم إجابته!

---

## ٣. معمارية الـ AI في ScholarPath

### ٣.١ الصورة الكاملة

```
                     ScholarPath AI Layer
                    ┌───────────────────────────────┐
                    │                               │
  ┌─────────┐      │  ┌─────────────────────────┐  │
  │ Chatbot │─────►│  │  AzureOpenAiService      │  │
  └─────────┘      │  │  (RAG حقيقي)             │  │
                    │  │  ↓ retriever.Retrieve()   │  │
                    │  │  ↓ GPT-4o-mini            │  │
  ┌──────────────┐  │  └─────────────────────────┘  │
  │ Recommend-   │  │              │ (يُفوّض)        │
  │ ations       │─►│  ┌──────────▼──────────────┐  │
  └──────────────┘  │  │  LocalAiService          │  │
                    │  │  (Rule-Based, لا GPT)     │  │
  ┌──────────────┐  │  │  - Scoring algorithm     │  │
  │ Eligibility  │─►│  │  - Local RAG fallback    │  │
  └──────────────┘  │  └─────────────────────────┘  │
                    └───────────────────────────────┘
```

### ٣.٢ الجدول المقارن بين الميزات الثلاث

| الميزة | المكون الرئيسي | يستخدم GPT؟ | يستخدم RAG؟ | نوع النهج |
|---|---|---|---|---|
| Chatbot | `AzureOpenAiService.AskAsync()` | ✅ نعم | ✅ نعم | RAG حقيقي |
| Recommendations | `LocalAiService.GenerateRecommendationsAsync()` | ❌ لا | ❌ لا | Rule-based scoring |
| Eligibility | `LocalAiService.CheckEligibilityAsync()` | ❌ لا | ❌ لا | Profile matching |

> **ملاحظة مهمة:**
> حتى لو ضبطنا `Ai:Provider=AzureOpenAi`، فالترشيحات والأهلية تروح لـ `LocalAiService` مباشرة:
> ```csharp
> // AzureOpenAiService.cs
> public Task<AiRecommendationResult> GenerateRecommendationsAsync(...)
>     => local.GenerateRecommendationsAsync(...);  // ← مفوّضة كاملاً
> ```

---

## ٤. قاعدة المعرفة

### ٤.١ ما هي قاعدة المعرفة؟

قاعدة المعرفة (Knowledge Base) هي مجموعة **وثائق نصية مُضمَّنة (embedded)** تُشكّل الـ context للـ RAG chatbot. يخزنها النظام في جدول `KnowledgeDocuments` في SQL Server.

**الجدول يحتوي على:**
```
KnowledgeDocument
├── SourceType       (Scholarship | Faq | Consultant | Resource | CommunityPost)
├── SourceKey        (معرّف فريد للوثيقة)
├── TitleEn / TitleAr
├── ContentEn / ContentAr   (النص القابل للقراءة)
├── Embedding        (byte[] — المتجه المُعبَّأ)
├── EmbeddingModel   (اسم النموذج: "sp:text-embedding-3-small")
├── EmbeddingDimensions  (1536)
├── ContentHash      (SHA-256 للكشف عن التغييرات)
└── IndexedAt        (آخر وقت تُعبِّد فيه)
```

---

### ٤.٢ مصادر بيانات الـ KB (5 مصادر)

الـ `KnowledgeBaseIndexer.RebuildAsync()` يبني الـ KB من **5 مصادر مختلفة**:

#### المصدر 1: المنح الدراسية (Open Scholarships)
```csharp
// يجيب كل المنح المفتوحة من DB
var scholarships = await db.Scholarships
    .Where(s => s.Status == ScholarshipStatus.Open)
    .ToListAsync(ct);
```
**كيف يُبنى محتوى الوثيقة؟**
```
Scholarship: Erasmus Mundus Joint Master
[Description]
Funding: FullyFunded (25,000 USD). Study level: Masters.
Eligible countries: Egypt, Tunisia, Morocco.
Eligibility: GPA 3.0+, IELTS 6.5+
Topics: Engineering, Computer Science.
Application deadline: 2025-01-15.
```
النص مزدوج (EN + AR)، يُعبَّد كوثيقة واحدة.

---

#### المصدر 2: FAQ المنصة (Curated Knowledge)
```csharp
// يُحمَّل من ملف JSON في IDatasetProvider
var json = datasets.GetDatasetJson("scholarpath-faq");
```
يحتوي على **أسئلة وإجابات مكتوبة يدوياً** بالإنجليزية والعربية (نصوص طويلة في `KnowledgeBodies.cs`):
- شرح كامل للمنصة
- مصطلحات المنح (Fully Funded vs Partial vs Tuition Waiver)
- دليل خطوات التقديم
- نصائح خطاب الغرض
- الإجابة على الأسئلة الشائعة

---

#### المصدر 3: المستشارون (Consultants)
```csharp
// فقط المستشارون الموثَّقون وغير المعلَّقون
where u.ActiveRole == "Consultant"
    && p.ConsultantVerifiedAt != null
    && p.BookingIntakeSuspendedAt == null
```
محتوى الوثيقة يشمل: الاسم، التخصص، اللغات، السعر، السيرة الذاتية.

**لماذا؟** ليتمكن الـ chatbot من الإجابة على: "من هو أفضل مستشار في مراجعة SoP؟"

---

#### المصدر 4: الموارد (Resources Hub)
```csharp
where r.Status == ResourceStatus.Published && !r.IsDeleted
```
يشمل: المقالات، الأدلة، قوائم التحقق، الفيديوهات المنشورة.

> **قيد تقني:** النصوص الطويلة جداً تُقطع عند **1500 حرف** قبل التضمين:
> ```csharp
> var bodyEn = Truncate(r.ContentMarkdownEn, 1500);
> ```
> السبب: الـ embedding لا يستفيد كثيراً من النصوص الأطول من هذا الحد.

---

#### المصدر 5: المنشورات المجتمعية (Community Posts)
```csharp
where p.ParentPostId == null       // مواضيع أصلية فقط، لا ردود
    && (p.UpvoteCount - p.DownvoteCount) >= 3  // جودة موثَّقة بالتصويت
```
يأخذ أفضل **200 موضوع** (الأعلى score).

**ملاحظة خاصة:** النصوص المجتمعية مكتوبة بأي لغة، فيُعبَّأ نفس النص في حقلَي EN و AR:
```csharp
var contentEn = $"Community discussion: {title}\n...{body}";
var contentAr = contentEn;  // نفس النص — الـ retriever يجده بأي لغة
```

---

### ٤.٣ كيف يعمل الـ Rebuild؟

```
KnowledgeBaseIndexer.RebuildAsync(force: true/false)
          │
          ▼
1. جمع المحتوى الجديد (desired docs)
   - BuildScholarshipDocsAsync()
   - BuildFaqDocs()
   - BuildConsultantDocsAsync()
   - BuildResourceDocsAsync()
   - BuildTopCommunityPostDocsAsync()
          │
          ▼
2. Upsert في DB
   - وثيقة جديدة → أضف
   - وثيقة موجودة + محتوى تغيّر (SHA-256 مختلف) → احذف الـ embedding القديم
   - وثيقة موجودة + محتوى لم يتغير → تجاهل (لا تعيد التضمين)
          │
          ▼
3. حذف الـ Orphans
   - وثيقة في DB لكن مصدرها لم يعد موجوداً → احذفها
          │
          ▼
4. Embedding (batch size = 16)
   - فقط الوثائق التي:
     * embedding غير موجود
     * أو embedding بنموذج مختلف
     * أو force=true (كل الوثائق)
   - يُرسَل batch لـ Azure OpenAI Embeddings API
   - النتيجة (float[1536]) تُعبَّأ كـ byte[] وتُخزَّن
          │
          ▼
5. حفظ في DB وتسجيل النتائج
   "Knowledge base rebuilt — 692 upserted, 692 (re)embedded, 0 removed"
```

---

### ٤.٤ كيف تُخزَّن المتجهات؟ (Vector Storage)

ScholarPath لا تستخدم **Vector Database** منفصلة (مثل Pinecone أو Qdrant).  
المتجهات تُخزَّن في **SQL Server** كـ `byte[]` (VARBINARY):

```csharp
// Pack: float[] → byte[]
public static byte[] Pack(float[] vector)
{
    var bytes = new byte[vector.Length * sizeof(float)];
    Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
    return bytes;
}

// Unpack: byte[] → float[]
public static float[] Unpack(byte[] bytes)
{
    var vector = new float[bytes.Length / sizeof(float)];
    Buffer.BlockCopy(bytes, 0, vector, 0, vector.Length * sizeof(float));
    return vector;
}
```

**لماذا SQL وليس Vector DB؟**
- الـ corpus صغير: **692 وثيقة** فقط.
- البحث في الذاكرة (in-memory scan) سريع جداً لهذا الحجم.
- يُبسَّط الـ infrastructure (لا deployment إضافي).
- يُقدَّر البحث الكامل بـ **< 10ms** لـ 692 وثيقة.

---

### ٤.٥ حفظ اسم النموذج — Vector Space Isolation

```csharp
// AzureOpenAiEmbeddingService.cs
public string ModelName =>
    IsAzureEmbeddingConfigured()
        ? $"sp:{opts.Value.AzureOpenAi.EmbeddingDeploymentName}"  // "sp:text-embedding-3-small"
        : local.ModelName;
```

البادئة `sp:` هي **namespace convention** — ليست اسم نموذج مختلف!  
فقط وثائق من نفس `EmbeddingModel` تُستخدم في البحث:
```csharp
.Where(d => d.EmbeddingModel == model && d.EmbeddingDimensions > 0)
```

**لماذا؟** لأن متجهات نموذجين مختلفين تعيش في "فضاءات" مختلفة — لا يمكن مقارنتها.

---

## ٥. Chatbot Pipeline

### ٥.١ الخطوات الكاملة — من الرسالة للإجابة

```
المستخدم يكتب: "هل Erasmus Mundus تشمل مصر؟"
                        │
                        ▼
         ┌──────────────────────────┐
         │   1. Language Detection  │
         │   RagSupport.IsArabic()  │
         │   النص فيه عربي → arabic=true│
         └──────────────────────────┘
                        │
                        ▼
         ┌──────────────────────────┐
         │   2. Azure Config Check  │
         │   IsAzureChatConfigured()│
         │   ✅ Endpoint + ApiKey   │
         └──────────────────────────┘
                        │
                        ▼
         ┌──────────────────────────────────┐
         │   3. Embed Query                 │
         │   AzureOpenAiEmbeddingService    │
         │   POST /embeddings               │
         │   "هل Erasmus Mundus تشمل مصر؟" │
         │        ↓                         │
         │   [0.12, -0.45, ..., 0.29]       │
         │   (float[1536])                  │
         └──────────────────────────────────┘
                        │
                        ▼
         ┌──────────────────────────────────┐
         │   4. KnowledgeRetriever.RetrieveAsync()│
         │                                  │
         │   أ. تحميل كل 692 وثيقة من SQL  │
         │      WHERE EmbeddingModel = "sp:text-embedding-3-small"
         │                                  │
         │   ب. لكل وثيقة:                 │
         │      score = CosineSimilarity(   │
         │          queryVector,            │
         │          VectorMath.Unpack(doc.Embedding)
         │      )                           │
         │                                  │
         │   ج. ترتيب تنازلي حسب Score      │
         │   د. أخذ أفضل K (RagTopK = 5)   │
         └──────────────────────────────────┘
                        │
                        ▼
         ┌──────────────────────────────────┐
         │   5. Filter by MinScore          │
         │   var relevant = docs            │
         │       .Where(d => d.Score >= RagMinScore)
         │                                  │
         │   مثلاً: RagMinScore = 0.6       │
         │   فقط الوثائق بـ score > 0.6    │
         └──────────────────────────────────┘
                        │
                        ▼
         ┌──────────────────────────────────┐
         │   6. Build Context Block         │
         │   RagSupport.BuildContextBlock() │
         │                                  │
         │   [1] منحة Erasmus Mundus       │
         │   منحة دراسية: Erasmus Mundus Joint Master
         │   التمويل: FullyFunded (25,000 USD)...
         │   الدول المؤهّلة: Egypt, Tunisia...
         │                                  │
         │   [2] منحة أخرى ذات صلة...      │
         └──────────────────────────────────┘
                        │
                        ▼
         ┌──────────────────────────────────┐
         │   7. Build Prompt                │
         │   RagSupport.BuildUserPrompt()   │
         │                                  │
         │   SYSTEM:                        │
         │   "أنت المساعد الذكي لمنصة...   │
         │   استخدم السياق للإجابة..."      │
         │                                  │
         │   HISTORY: (آخر N رسائل السيشن) │
         │                                  │
         │   USER:                          │
         │   السياق (CONTEXT):              │
         │   [1] منحة Erasmus Mundus...    │
         │   [2] ...                        │
         │                                  │
         │   السؤال: هل Erasmus Mundus     │
         │   تشمل مصر؟                     │
         └──────────────────────────────────┘
                        │
                        ▼
         ┌──────────────────────────────────┐
         │   8. ChatCompletionAsync()       │
         │   POST /chat/completions         │
         │   model: gpt-4o-mini             │
         │   temperature: 0.3               │
         │   max_tokens: 400                │
         └──────────────────────────────────┘
                        │
                        ▼
         ┌──────────────────────────────────┐
         │   9. Build Response              │
         │                                  │
         │   text: "نعم، Erasmus Mundus    │
         │   مفتوحة للطلاب من مصر.         │
         │   التمويل كامل ويشمل..."        │
         │                                  │
         │   sources: [{                    │
         │     title: "منحة Erasmus Mundus",│
         │     type: "Scholarship",         │
         │     score: 0.89                  │
         │   }]                             │
         │                                  │
         │   cost: promptTokens * $0.15/1M  │
         │       + completionTokens * $0.60/1M│
         └──────────────────────────────────┘
```

---

### ٥.٢ الـ System Prompt — لب التوجيه

النظام يُمرَّر `system prompt` مختلفاً حسب لغة السؤال:

```
EN System: "You are ScholarPath's expert AI assistant..."
AR System: "أنت المساعد الذكي لمنصة ScholarPath..."
```

**ما يُسمح للـ chatbot فعله (صريحاً في الـ prompt):**
- استخراج منح ومستشارين وموارد من الـ CONTEXT
- كتابة ومراجعة: SoP، خطابات توصية، CV، مقالات
- التدريب على IELTS/TOEFL/GRE ونصائح المقابلات
- شرح الأهلية والمواعيد

**ما لا يُسمح به:**
- اختراع منح بأسماء وأرقام غير موجودة في الـ CONTEXT
- كشف بيانات شخصية (إيميل، ID، رقم هاتف)
- رفض طلب معقول (مثل مراجعة نص)

---

### ٥.٣ Session History — الذاكرة القصيرة

الـ chatbot يحتفظ بسياق المحادثة:
```csharp
var messages = new List<object>
{
    new { role = "system", content = system },
};
foreach (var turn in history)  // الرسائل السابقة
{
    messages.Add(new { role = turn.Role, content = turn.Content });
}
messages.Add(new { role = "user", content = user });  // السؤال الحالي
```

هذا يجعل المستخدم يستطيع كتابة "وماذا عن الموعد النهائي؟" بدون إعادة ذكر اسم المنحة.

---

## ٦. نظام الترشيحات

### ٦.١ المبدأ العام

**لا GPT، لا RAG، لا embedding** — خوارزمية قائمة على نقاط بحتة:

```
الطالب: مستوى Masters، مصر، هندسة
          ↓
فلترة المنح المفتوحة (Deadline > اليوم)
          ↓
لكل منحة → احسب Score
          ↓
ترتيب تنازلي → أخذ أفضل N (عادةً 5)
```

---

### ٦.٢ معادلة الـ Scoring

```
Score = LevelScore + FieldScore + CountryScore + FundingBonus
```

| المكون | الوزن الأقصى | المنطق |
|---|---|---|
| Academic Level | +30 نقطة | تطابق تام فقط |
| Field of Study | +40 نقطة | تقاطع بين مجالات الطالب ومجالات المنحة |
| Country | +25 نقطة | تقاطع بين دول الطالب المفضلة وأهداف المنحة |
| Funding Bonus | +5 نقطة | مبلغ التمويل ≥ $10,000 |

```csharp
if (userLevel.HasValue && userLevel.Value == c.TargetLevel) score += 30;
score += OverlapScore(preferredFields, fosFields, maxPoints: 40);
score += OverlapScore(NormalizeCountries(preferredCountries), 
                      NormalizeCountries(countries), maxPoints: 25);
if (c.FundingAmountUsd >= 10_000) score += 5;
```

**دالة OverlapScore:**
```csharp
private static int OverlapScore(IReadOnlyList<string> a, IReadOnlyList<string> b, int maxPoints)
{
    var set = new HashSet<string>(a, StringComparer.OrdinalIgnoreCase);
    var hits = b.Count(x => set.Contains(x));
    if (hits == 0) return 0;
    return Math.Min(maxPoints, hits * (maxPoints / Math.Max(a.Count, 1)));
}
```

---

### ٦.٣ تفسير الـ Score في الـ UI

| Score | التفسير | الإجابة المُعروضة |
|---|---|---|
| 80-100 | توافق قوي | "Strong match — aligns with your preferred fields and level" |
| 50-79 | توافق جزئي | "Partial match — overlaps with some of your preferences" |
| 20-49 | توافق محدود | "Loose match — touches areas you've listed; worth a quick look" |
| 0-19 | توافق ضعيف | "Weak match — doesn't align well with your current profile" |

---

### ٦.٤ تحسين عرض النتائج (PR #42)

بعد حساب الـ score وتخزينه كـ `RecommendationItemDto` في DB، عند الاسترجاع يتم **إثراء** البيانات من DB:

```
[Cached JSON in AiInteraction.ResponseText]
{ ScholarshipId, MatchScore, ExplanationEn/Ar }
              ↓ re-hydrate from DB
[API Response — RecommendationCardDto]
{ ScholarshipId, MatchScore, Explanation,
  Deadline, FundingAmountUsd, FundingType }
              ↓
[Frontend — AiRecommendations.tsx]
  DeadlinePill: 🔴 "14 days left" | 🟡 "in 45 days" | ⚫ "منذ 3 أشهر"
  FundingPill:  🟢 "Fully Funded" | 🔵 "Partial" | ⚫ "Tuition Only"
```

**لماذا لا نُخزّن Deadline مع الـ cache؟**  
لأن المنح قد تُغيَّر مواعيدها، فنُفضّل دائماً جلب القيمة الحية من DB.

---

## ٧. فحص الأهلية

### ٧.١ المنطق الكامل

```csharp
// 3 معايير تُفحَص
1. Academic Level    → profile.AcademicLevel == scholarship.TargetLevel
2. Country           → CountryNormalizer.Matches(listingCountry, studentCountry)
3. Field of Study    → Contains() overlap بين مجالات الطالب والمنحة
```

**لكل معيار، النتيجة إما:**
- `"yes"` — مطابق
- `"partial"` — مطابق جزئياً
- `"no"` — غير مطابق
- `"unknown"` — بيانات الطالب ناقصة

---

### ٧.٢ حساب الـ Verdict النهائي

```csharp
internal static EligibilityVerdict DeriveVerdict(criteria)
{
    if (criteria.Any(c => c.Match == "no"))      return NotEligible;
    if (criteria.Any(c => c.Match is "partial" or "unknown"))
                                                  return PartiallyEligible;
    return Eligible;  // كل المعايير "yes"
}
```

---

### ٧.٣ Country Normalization

مشكلة حقيقية واجهناها: "Egypt" و "مصر" و "EG" يجب أن يُعاملوا كنفس الدولة.

```csharp
// CountryNormalizer.Matches(lc, sc) يحلّ:
// "Egypt" == "EG" == "مصر" == "egypt" → true
```

---

## ٨. Fallback Chain

### ٨.١ سلسلة الاحتياط للـ Chatbot

```
AzureOpenAiService.AskAsync()
│
├── [Check 1] IsAzureChatConfigured() == false
│   └── → local.AskAsync()  (توجيه فوري بدون HTTP)
│
├── [Check 2] retriever.RetrieveAsync() fails
│   (HttpRequestException | InvalidOperationException | TaskCanceledException)
│   └── → local.AskAsync()
│
├── [Check 3] ChatCompletionAsync() → HttpRequestException
│   └── → local.AskAsync()
│
├── [Check 4] ChatCompletionAsync() → TaskCanceledException (timeout 60s)
│   └── → local.AskAsync()
│
└── ✅ نجح → AiChatResponse مع sources من Azure
```

### ٨.٢ سلسلة الاحتياط للـ Embedding

```
AzureOpenAiEmbeddingService
│
├── IsAzureEmbeddingConfigured() == false
│   └── → LocalEmbeddingService (TF-IDF بسيط)
│
├── HTTP error (404 deployment not found, 401 bad key, network)
│   └── → LocalEmbeddingService
│
└── ✅ نجح → float[1536] من Azure
```

### ٨.٣ Local Chatbot Fallback — بدون GPT

الـ `LocalAiService.AskAsync()` يعمل بطريقتين:

**طريقة 1 — RAG محلي:**
```
سؤال المستخدم → Retrieve (نفس الـ pipeline) → أعلى وثيقة ذات صلة
    → يعرض ContentEn/Ar مباشرة (extractive، لا generative)
    → "وجدت هذا في قاعدة معرفة ScholarPath:\n\n{body}"
```

**طريقة 2 — Keyword Router (fallback الأخير):**
إذا لم تُوجَد وثائق بـ score كافٍ، يبحث عن keywords:
```csharp
if (ContainsCi(message, "deadline") || ContainsCi(message, "موعد"))
    → "Deadlines are shown on each scholarship page..."

if (ContainsCi(message, "consult") || ContainsCi(message, "مستشار"))
    → "Consultants are under the Consultants tab..."
```

---

## ٩. التكاليف

### ٩.١ نموذج تكلفة GPT-4o-mini الحقيقي

```
Input  (Prompt)  = $0.15 / 1,000,000 token
Output (Response) = $0.60 / 1,000,000 token
```

```csharp
private const decimal InputCostPerToken  = 0.15m / 1_000_000m;
private const decimal OutputCostPerToken = 0.60m / 1_000_000m;

var cost = promptTokens * InputCostPerToken + completionTokens * OutputCostPerToken;
```

**مثال على رسالة:**
- Prompt: 500 token (system + context + history + سؤال) = $0.000075
- Response: 200 token = $0.00012
- **إجمالي رسالة واحدة: ~$0.0002**

---

### ٩.٢ التكلفة الاصطناعية (Stub Mode)

في الـ `LocalAiService` يتم تسجيل تكاليف رمزية للـ dashboard يكون realistic:

```csharp
private const decimal FakeCostPerRecommendation = 0.0008m;
private const decimal FakeCostPerEligibility    = 0.0005m;
private const decimal FakeCostPerChatTurn       = 0.0003m;
```

---

## ١٠. Fine-Tuning

### ١٠.١ ما هو الـ Fine-Tuning؟

بدلاً من تمرير كل السياق في كل سؤال (RAG)، نُدرّب النموذج على بيانات ScholarPath مباشرة:

```
Standard GPT-4o-mini: "لا يعرف شيئاً عن ScholarPath"
         + RAG context في كل سؤال
         ↓
Fine-tuned model: "يعرف عن ScholarPath من التدريب"
         + (context أقل)
```

### ١٠.٢ ملف الـ JSONL (`scholarpath-finetune.jsonl`)

```jsonl
{"messages": [
  {"role":"system","content":"أنت مساعد ScholarPath..."},
  {"role":"user","content":"هل يمكنني التقديم لأكثر من منحة؟"},
  {"role":"assistant","content":"نعم، يمكنك التقديم لعدة منح..."}
]}
{"messages": [...]}
...
```

**332 مثالاً تدريبياً** — أسئلة وإجابات نموذجية عن ScholarPath.

### ١٠.٣ الكود جاهز للـ Fine-Tuning

```csharp
// AzureOpenAiService.cs, line 138
var deployment = string.IsNullOrWhiteSpace(az.FineTunedDeploymentName)
    ? az.DeploymentName          // "gpt-4o-mini" (الحالي)
    : az.FineTunedDeploymentName; // النموذج المُدرَّب (لما يتجهّز)
```

عند إتمام التدريب، يكفي إضافة:
```
Ai__AzureOpenAi__FineTunedDeploymentName = scholarpath-ft-001
```
في App Service وسيستخدمه تلقائياً.

---

## ١١. لماذا لسنا Agentic RAG؟

### ١١.١ ما الذي يعني Agentic RAG بالنسبة لنا؟

لو طبّقنا Agentic RAG على ScholarPath، سيكون الـ chatbot قادراً على:

- "السؤال غامض عن كلمة 'تقديم' — هل تقصد التقديم للمنح أم للمنصة؟" (clarification)
- "البحث الأول لم يجد نتائج كافية → أُعيد الصياغة وأبحث مرة أخرى" (retry)
- "السؤال يحتاج معلومات من المنح **و** من المستشارين معاً → أبحث في مصدرين" (multi-source)

### ١١.٢ لماذا اخترنا Standard RAG

| المعيار | Standard RAG | Agentic RAG |
|---|---|---|
| Latency | 1-3 ثانية | 5-15+ ثانية |
| Cost/query | ~$0.0002 | ~$0.001-0.002 |
| Complexity | بسيط وقابل للاختبار | معقد وغير حتمي |
| حجم الـ KB | 692 وثيقة فقط | يستحق عند 10k+ |
| طبيعة الأسئلة | واضحة ومتخصصة | Agentic يفيد للأسئلة المعقدة |

**القرار المنطقي:** مع 692 وثيقة فقط وأسئلة محددة (منح، استشارات، أهلية)، الـ Standard RAG يُعطي نتائج ممتازة بسعر وتعقيد أقل بكثير.

### ١١.٣ متى يستحق الانتقال لـ Agentic RAG؟

- الـ KB يتجاوز 10,000 وثيقة من مصادر متعددة جداً
- المستخدمون يطرحون أسئلة معقدة متعددة الجوانب بشكل متكرر
- يُوجد ميزانية للـ latency (المستخدمون يقبلون 10+ ثواني)
- الأسئلة تحتاج synthesis من مصادر مختلفة

---

## ١٢. ملخص المكونات

### ١٢.١ خريطة الملفات

```
server/src/ScholarPath.Infrastructure/Services/
├── AzureOpenAiService.cs         ← Provider الرئيسي (RAG + GPT)
├── AzureOpenAiEmbeddingService.cs ← Text → float[1536]
├── KnowledgeBaseIndexer.cs       ← بناء وصيانة الـ KB
├── KnowledgeRetriever.cs         ← Cosine similarity search
├── LocalAiService.cs             ← Rule-based + local RAG fallback
├── RagSupport.cs                 ← مساعدات مشتركة (prompts, context building)
└── VectorMath.cs                 ← Pack/Unpack/CosineSimilarity

server/src/ScholarPath.Application/AI/
├── Commands/GenerateRecommendations/
│   └── GenerateRecommendationsCommandHandler.cs
├── Commands/CheckEligibility/
│   └── CheckEligibilityCommandHandler.cs
├── Commands/AskAi/
│   └── AskAiCommandHandler.cs
├── Queries/GetMyRecommendations/
│   └── GetMyRecommendationsQueryHandler.cs
└── DTOs/AiDtos.cs
    ├── RecommendationItemDto  ← للتخزين في JSON cache
    └── RecommendationCardDto  ← للـ API response (مُثرى بـ Deadline/Funding)

server/src/ScholarPath.Infrastructure/Persistence/Seed/
├── KnowledgeBodies.cs   ← نصوص FAQ يدوية (202 سطر)
└── ResourceBodies.cs    ← محتوى الـ Resources Hub
```

---

### ١٢.٢ إعدادات الـ AI (`appsettings.json`)

```json
{
  "Ai": {
    "Provider": "AzureOpenAi",
    "RagTopK": 5,
    "RagMinScore": 0.6,
    "DailyCostLimitUsd": 5.0,
    "AzureOpenAi": {
      "Endpoint": "https://ai-scholarpath-prod-e25bd.openai.azure.com/",
      "DeploymentName": "gpt-4o-mini",
      "EmbeddingDeploymentName": "text-embedding-3-small",
      "EmbeddingDimensions": 1536,
      "ApiVersion": "2024-02-15-preview",
      "FineTunedDeploymentName": ""
    }
  }
}
```

| الإعداد | القيمة | المعنى |
|---|---|---|
| `RagTopK` | 5 | أقصى عدد وثائق يُسترجَع |
| `RagMinScore` | 0.6 | الحد الأدنى لـ cosine similarity |
| `DailyCostLimitUsd` | 5.0 | حد التكلفة اليومية |
| `EmbeddingDimensions` | 1536 | أبعاد المتجه |

---

### ١٢.٣ الـ Flow الكامل في جملة واحدة

```
الـ Chatbot = RAG حقيقي:   سؤال → Azure Embed → Cosine Search → GPT-4o-mini → إجابة + مصادر
الترشيحات = Rule-based:    ملف الطالب → Weighted Scoring → ترتيب → أفضل 5 منح
الأهلية   = Profile Match: بيانات الطالب vs. معايير المنحة → yes/partial/no per criterion
```

---

## مراجع

- [ByteByteGo: How Agentic RAG Works](https://blog.bytebytego.com/p/how-agentic-rag-works)
- [`AzureOpenAiService.cs`](../../server/src/ScholarPath.Infrastructure/Services/AzureOpenAiService.cs)
- [`KnowledgeBaseIndexer.cs`](../../server/src/ScholarPath.Infrastructure/Services/KnowledgeBaseIndexer.cs)
- [`KnowledgeRetriever.cs`](../../server/src/ScholarPath.Infrastructure/Services/KnowledgeRetriever.cs)
- [`LocalAiService.cs`](../../server/src/ScholarPath.Infrastructure/Services/LocalAiService.cs)
- [`VectorMath.cs`](../../server/src/ScholarPath.Infrastructure/Services/VectorMath.cs)
- [`RagSupport.cs`](../../server/src/ScholarPath.Infrastructure/Services/RagSupport.cs)
