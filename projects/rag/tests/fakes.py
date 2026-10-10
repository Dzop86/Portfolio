"""A stand-in for the Anthropic client: it keeps each request and answers what the test wants."""
from types import SimpleNamespace as NS


def text(t, citations=None):
    return NS(type="text", text=t, citations=citations)


def cite(index, start, end, quote="..."):
    """A search_result_location citation, as the Messages API returns them."""
    return NS(type="search_result_location", search_result_index=index, start_block_index=start, end_block_index=end,
              cited_text=quote, source="", title="")


class FakeClient:
    def __init__(self, reply):
        """``reply(request) -> (content blocks, stop_reason)``"""
        self.requests = []
        self._reply = reply
        self.beta = NS(messages=NS(create=self._create))

    def _create(self, **request):
        self.requests.append(request)
        content, stop = self._reply(request)
        return NS(content=content, stop_reason=stop, model=request["model"])
